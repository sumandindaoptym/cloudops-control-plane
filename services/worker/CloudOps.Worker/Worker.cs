using CloudOps.Shared.Services;
using CloudOps.Shared.Data;
using CloudOps.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR.Client;

namespace CloudOps.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMessageBus _messageBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly HubConnection _hubConnection;

    public Worker(ILogger<Worker> logger, IMessageBus messageBus, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _messageBus = messageBus;
        _serviceProvider = serviceProvider;
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5056/hubs/tasks")
            .WithAutomaticReconnect()
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _hubConnection.StartAsync(stoppingToken);
            _logger.LogInformation("SignalR connection established");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not connect to SignalR hub, continuing without real-time updates");
        }

        _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _messageBus.ReceiveAsync(null, stoppingToken);
                
                if (message != null)
                {
                    _logger.LogInformation("Processing task {TaskId} of type {Type}", message.TaskId, message.Type);
                    await ProcessTaskAsync(message, stoppingToken);
                    await _messageBus.CompleteAsync(message, stoppingToken);
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessTaskAsync(Shared.DTOs.TaskMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudOpsDbContext>();

        var task = await db.Tasks.FindAsync(new object[] { message.TaskId }, cancellationToken);
        if (task == null) return;

        task.Status = "running";
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await SendUpdate(message.TaskId, "running", null);

        try
        {
            switch (message.Type)
            {
                case "deployment":
                    await ProcessDeploymentAsync(task, message, cancellationToken);
                    break;
                case "db_backup":
                    await ProcessBackupAsync(task, message, cancellationToken);
                    break;
                case "db_restore":
                    await ProcessRestoreAsync(task, message, cancellationToken);
                    break;
                case "restart_pods":
                    await ProcessRestartPodsAsync(task, message, cancellationToken);
                    break;
                case "create_sandbox":
                    await ProcessSandboxAsync(task, message, cancellationToken);
                    break;
            }

            task.Status = "completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId} failed", task.Id);
            task.Status = "failed";
            await AddStep(task, "error", $"Task failed: {ex.Message}", 0);
        }

        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await SendUpdate(task.Id, task.Status, null);
    }

    private async Task ProcessDeploymentAsync(TaskEntity task, Shared.DTOs.TaskMessage message, CancellationToken ct)
    {
        await AddStep(task, "validate", "Validating deployment parameters", 10);
        await Task.Delay(1000, ct);
        
        await AddStep(task, "plan", "Generating deployment plan", 40);
        await Task.Delay(1500, ct);
        var planPath = Path.Combine("data/artifacts", $"plan-{task.Id}.json");
        await File.WriteAllTextAsync(planPath, $"{{{{\"taskId\": \"{task.Id}\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}}}", ct);
        
        await AddStep(task, "apply", "Applying changes (simulated)", 80);
        await Task.Delay(2000, ct);
        
        await AddStep(task, "complete", "Deployment successful", 100);
    }

    private async Task ProcessBackupAsync(TaskEntity task, Shared.DTOs.TaskMessage message, CancellationToken ct)
    {
        await AddStep(task, "prepare", "Preparing backup", 20);
        await Task.Delay(1000, ct);
        
        await AddStep(task, "backup", "Creating backup (simulated)", 60);
        await Task.Delay(2000, ct);
        var backupPath = Path.Combine("data/backups", $"backup-{task.Id}.sql");
        await File.WriteAllTextAsync(backupPath, $"-- Backup created at {DateTime.UtcNow:O}", ct);
        
        await AddStep(task, "complete", "Backup successful", 100);
    }

    private async Task ProcessRestoreAsync(TaskEntity task, Shared.DTOs.TaskMessage message, CancellationToken ct)
    {
        await AddStep(task, "validate", "Validating artifact", 20);
        await Task.Delay(1000, ct);
        
        await AddStep(task, "restore", "Restoring database (simulated)", 70);
        await Task.Delay(2500, ct);
        
        await AddStep(task, "complete", "Restore successful", 100);
    }

    private async Task ProcessRestartPodsAsync(TaskEntity task, Shared.DTOs.TaskMessage message, CancellationToken ct)
    {
        await AddStep(task, "connect", "Connecting to Kubernetes", 30);
        await Task.Delay(1000, ct);
        
        await AddStep(task, "restart", "Restarting pods (simulated)", 70);
        await Task.Delay(1500, ct);
        
        await AddStep(task, "complete", "Pods restarted successfully", 100);
    }

    private async Task ProcessSandboxAsync(TaskEntity task, Shared.DTOs.TaskMessage message, CancellationToken ct)
    {
        await AddStep(task, "create", "Creating sandbox environment", 50);
        await Task.Delay(1500, ct);
        
        await AddStep(task, "configure", "Configuring TTL auto-expiry", 80);
        await Task.Delay(1000, ct);
        
        await AddStep(task, "complete", "Sandbox created successfully", 100);
    }

    private async Task AddStep(TaskEntity task, string name, string message, int percent)
    {
        var step = new TaskStep
        {
            TaskId = task.Id,
            Name = name,
            Status = percent >= 100 ? "completed" : "running",
            StartedAt = DateTime.UtcNow,
            CompletedAt = percent >= 100 ? DateTime.UtcNow : null,
            PercentComplete = percent,
            Logs = new List<string> { message }
        };
        
        task.Steps.Add(step);
        
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudOpsDbContext>();
        db.Tasks.Update(task);
        await db.SaveChangesAsync();
        
        await SendUpdate(task.Id, step.Status, message, percent);
    }

    private async Task SendUpdate(Guid taskId, string status, string? message, int? percent = null)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("SendTaskUpdate", new
                {
                    taskId,
                    status,
                    message,
                    percent,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send SignalR update");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _hubConnection.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
