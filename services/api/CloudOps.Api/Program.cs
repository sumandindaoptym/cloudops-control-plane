using Microsoft.EntityFrameworkCore;
using CloudOps.Shared.Data;
using CloudOps.Shared.Services;
using CloudOps.Shared.Models;
using CloudOps.Shared.DTOs;
using Serilog;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

var demoMode = builder.Configuration.GetValue<bool>("DEMO_MODE", true);
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../data/platform/platform.db");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? $"Data Source={dbPath}";

builder.Services.AddDbContext<CloudOpsDbContext>(options =>
{
    if (demoMode || connectionString.Contains("Data Source"))
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<IMessageBus, InMemoryMessageBus>();
builder.Services.AddHostedService<CloudOps.Api.TaskWorker>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CloudOpsDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHub<TaskHub>("/hubs/tasks");

app.MapGet("/api/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.MapPost("/api/deployments", async (DeploymentRequest request, CloudOpsDbContext db, IMessageBus bus, IHubContext<TaskHub> hubContext) =>
{
    var task = new TaskEntity
    {
        Type = "deployment",
        EntityId = request.EnvId.ToString(),
        Status = "queued",
        Metadata = new Dictionary<string, string>
        {
            ["projectId"] = request.ProjectId.ToString(),
            ["envId"] = request.EnvId.ToString(),
            ["templateId"] = request.TemplateId
        }
    };

    db.Tasks.Add(task);
    await db.SaveChangesAsync();

    var message = new TaskMessage
    {
        Type = "deployment",
        SessionId = request.EnvId.ToString(),
        EntityId = request.EnvId.ToString(),
        TaskId = task.Id,
        Payload = new Dictionary<string, object>
        {
            ["projectId"] = request.ProjectId,
            ["envId"] = request.EnvId,
            ["templateId"] = request.TemplateId,
            ["parameters"] = request.Parameters
        }
    };

    await bus.PublishAsync(message);
    await hubContext.Clients.All.SendAsync("TaskCreated", new { taskId = task.Id });
    return Results.Ok(new { taskId = task.Id });
});

app.MapPost("/api/db/{engine}/{instanceId}/backup", async (string engine, string instanceId, CloudOpsDbContext db, IMessageBus bus) =>
{
    var task = new TaskEntity { Type = "db_backup", EntityId = instanceId, Status = "queued", Metadata = new Dictionary<string, string> { ["engine"] = engine, ["instanceId"] = instanceId } };
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    await bus.PublishAsync(new TaskMessage { Type = "db_backup", SessionId = instanceId, EntityId = instanceId, TaskId = task.Id, Payload = new Dictionary<string, object> { ["engine"] = engine, ["instanceId"] = instanceId } });
    return Results.Ok(new { taskId = task.Id });
});

app.MapPost("/api/db/{engine}/{instanceId}/restore", async (string engine, string instanceId, RestoreRequest request, CloudOpsDbContext db, IMessageBus bus) =>
{
    var task = new TaskEntity { Type = "db_restore", EntityId = instanceId, Status = "queued", Metadata = new Dictionary<string, string> { ["engine"] = engine, ["instanceId"] = instanceId, ["artifactId"] = request.ArtifactId } };
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    await bus.PublishAsync(new TaskMessage { Type = "db_restore", SessionId = instanceId, EntityId = instanceId, TaskId = task.Id, Payload = new Dictionary<string, object> { ["engine"] = engine, ["instanceId"] = instanceId, ["artifactId"] = request.ArtifactId, ["targetDatabase"] = request.TargetDatabase } });
    return Results.Ok(new { taskId = task.Id });
});

app.MapPost("/api/k8s/workloads/{ns}/{name}:restart", async (string ns, string name, CloudOpsDbContext db, IMessageBus bus) =>
{
    var entityId = $"{ns}/{name}";
    var task = new TaskEntity { Type = "restart_pods", EntityId = entityId, Status = "queued", Metadata = new Dictionary<string, string> { ["namespace"] = ns, ["workloadName"] = name } };
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    await bus.PublishAsync(new TaskMessage { Type = "restart_pods", SessionId = entityId, EntityId = entityId, TaskId = task.Id, Payload = new Dictionary<string, object> { ["namespace"] = ns, ["workloadName"] = name } });
    return Results.Ok(new { taskId = task.Id });
});

app.MapPost("/api/sandboxes", async (SandboxRequest request, CloudOpsDbContext db, IMessageBus bus) =>
{
    var task = new TaskEntity { Type = "create_sandbox", EntityId = request.ProjectId.ToString(), Status = "queued", Metadata = new Dictionary<string, string> { ["projectId"] = request.ProjectId.ToString(), ["ttlMinutes"] = request.TtlMinutes.ToString() } };
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    await bus.PublishAsync(new TaskMessage { Type = "create_sandbox", SessionId = request.ProjectId.ToString(), EntityId = request.ProjectId.ToString(), TaskId = task.Id, Payload = new Dictionary<string, object> { ["projectId"] = request.ProjectId, ["ttlMinutes"] = request.TtlMinutes } });
    return Results.Ok(new { taskId = task.Id });
});

app.MapGet("/api/tasks", async (CloudOpsDbContext db) => Results.Ok(await db.Tasks.OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync()));
app.MapGet("/api/tasks/{id}", async (Guid id, CloudOpsDbContext db) => await db.Tasks.FindAsync(id) is TaskEntity task ? Results.Ok(task) : Results.NotFound());
app.MapGet("/api/projects", async (CloudOpsDbContext db) => Results.Ok(await db.Projects.Include(p => p.Environments).ToListAsync()));
app.MapGet("/api/cost/estimate", () => Results.Ok(new { monthlyCost = 450.00, breakdown = new[] { new { resource = "Compute (4 vCPU, 16GB RAM)", cost = 250.00 }, new { resource = "Storage (500GB)", cost = 100.00 }, new { resource = "Network (1TB egress)", cost = 100.00 } } }));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run("http://0.0.0.0:5056");

public class TaskHub : Hub
{
    public async Task SubscribeToTask(string taskId) => await Groups.AddToGroupAsync(Context.ConnectionId, taskId);
}
