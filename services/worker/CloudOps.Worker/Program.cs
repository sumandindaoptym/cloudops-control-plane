using CloudOps.Worker;
using CloudOps.Shared.Data;
using CloudOps.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.UseSerilog();

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
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
