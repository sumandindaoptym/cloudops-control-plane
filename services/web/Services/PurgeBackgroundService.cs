using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using CloudOps.Web.Hubs;
using CloudOps.Web.Models;
using ActivityLog = CloudOps.Web.Models.ActivityLog;

namespace CloudOps.Web.Services;

public class PurgeJob
{
    public string PurgeId { get; set; } = string.Empty;
    public PurgeRequest Request { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
}

public interface IPurgeQueue
{
    ValueTask QueuePurgeAsync(PurgeJob job);
    ValueTask<PurgeJob> DequeueAsync(CancellationToken cancellationToken);
    int Count { get; }
}

public class PurgeQueue : IPurgeQueue
{
    private readonly Channel<PurgeJob> _queue = Channel.CreateUnbounded<PurgeJob>();

    public async ValueTask QueuePurgeAsync(PurgeJob job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    public async ValueTask<PurgeJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
    
    public int Count => _queue.Reader.Count;
}

public class PurgeBackgroundService : BackgroundService
{
    private readonly IPurgeQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PurgeHub> _hubContext;
    private readonly IRunningJobsTracker _jobsTracker;
    private readonly ILogger<PurgeBackgroundService> _logger;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private const int MaxConcurrentJobs = 5;

    public PurgeBackgroundService(
        IPurgeQueue queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<PurgeHub> hubContext,
        IRunningJobsTracker jobsTracker,
        ILogger<PurgeBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _jobsTracker = jobsTracker;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(MaxConcurrentJobs, MaxConcurrentJobs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Purge Background Service started (max concurrent jobs: {MaxJobs})", MaxConcurrentJobs);
        var runningTasks = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                runningTasks.RemoveAll(t => t.IsCompleted);
                
                var job = await _queue.DequeueAsync(stoppingToken);
                
                await _concurrencySemaphore.WaitAsync(stoppingToken);
                
                var task = ProcessPurgeJobAsync(job, stoppingToken)
                    .ContinueWith(_ => _concurrencySemaphore.Release(), TaskScheduler.Default);
                runningTasks.Add(task);
                
                _logger.LogInformation("Started purge job {PurgeId}. Active jobs: {ActiveJobs}", 
                    job.PurgeId, MaxConcurrentJobs - _concurrencySemaphore.CurrentCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting purge job");
            }
        }

        if (runningTasks.Count > 0)
        {
            _logger.LogInformation("Waiting for {Count} running purge jobs to complete...", runningTasks.Count);
            await Task.WhenAll(runningTasks);
        }

        _logger.LogInformation("Purge Background Service stopped");
    }

    private async Task ProcessPurgeJobAsync(PurgeJob job, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var purgeId = job.PurgeId;
        Guid? activityId = null;

        var runningJob = new RunningPurgeJob
        {
            PurgeId = purgeId,
            UserId = job.UserId,
            UserEmail = job.UserEmail,
            NamespaceName = job.Request.NamespaceName ?? job.Request.Namespace,
            EntityName = job.Request.EntityName,
            EntityType = job.Request.EntityType,
            TopicSubscriptionName = job.Request.TopicSubscriptionName,
            Status = "Starting",
            StartTime = startTime
        };
        _jobsTracker.AddJob(runningJob);

        try
        {
            await SendUpdate(purgeId, "started", "Connecting to Service Bus...", 5);
            _jobsTracker.AddLog(purgeId, "Connecting to Service Bus...");

            using var scope = _scopeFactory.CreateScope();
            var runtimeService = scope.ServiceProvider.GetRequiredService<IServiceBusRuntimeService>();
            var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();

            var isTopic = job.Request.EntityType.Equals("topic", StringComparison.OrdinalIgnoreCase);
            var subResourceName = isTopic && !string.IsNullOrEmpty(job.Request.TopicSubscriptionName)
                ? $"{job.Request.EntityName}/{job.Request.TopicSubscriptionName}"
                : job.Request.EntityName;

            var activity = await activityService.LogActivityAsync(new ActivityLog
            {
                UserId = job.UserId,
                UserEmail = job.UserEmail,
                TaskName = "Purge DLQ",
                TaskType = "Service Bus",
                SubscriptionName = job.SubscriptionName,
                SubscriptionId = job.Request.SubscriptionId,
                ResourceName = job.Request.NamespaceName ?? job.Request.Namespace,
                SubResourceName = subResourceName,
                Status = "Running",
                StartTime = startTime
            });
            activityId = activity.Id;
            
            _jobsTracker.UpdateJob(purgeId, j => 
            {
                j.ActivityId = activityId;
                j.Status = "Running";
            });

            var result = await runtimeService.PurgeDlqFastAsync(
                job.Request,
                job.AccessToken,
                async (log, progress, purgedCount) =>
                {
                    _jobsTracker.AddLog(purgeId, log);
                    _jobsTracker.UpdateJob(purgeId, j =>
                    {
                        j.Progress = progress;
                        j.TotalPurged = purgedCount;
                    });
                    await SendUpdate(purgeId, "progress", log, progress, purgedCount);
                },
                cancellationToken);

            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

            if (result.Success)
            {
                await activityService.UpdateActivityAsync(activityId.Value, "Completed", result.TotalPurged);
                _jobsTracker.AddLog(purgeId, $"Purge completed. Total messages purged: {result.TotalPurged}");
                _jobsTracker.UpdateJob(purgeId, j =>
                {
                    j.Status = "Completed";
                    j.Progress = 100;
                    j.TotalPurged = result.TotalPurged;
                    j.EndTime = DateTime.UtcNow;
                });
                await SendUpdate(purgeId, "completed", 
                    $"Purge completed. Total messages purged: {result.TotalPurged}", 100,
                    result.TotalPurged, elapsed);
            }
            else
            {
                await activityService.UpdateActivityAsync(activityId.Value, "Failed", null, result.Error);
                _jobsTracker.AddLog(purgeId, $"Purge failed: {result.Error}", "Error");
                _jobsTracker.UpdateJob(purgeId, j =>
                {
                    j.Status = "Failed";
                    j.EndTime = DateTime.UtcNow;
                });
                await SendUpdate(purgeId, "failed", result.Error ?? "Unknown error", 100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in purge job {PurgeId}", purgeId);
            
            _jobsTracker.AddLog(purgeId, $"Error: {ex.Message}", "Error");
            _jobsTracker.UpdateJob(purgeId, j =>
            {
                j.Status = "Failed";
                j.EndTime = DateTime.UtcNow;
            });
            
            if (activityId.HasValue)
            {
                using var scope = _scopeFactory.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();
                await activityService.UpdateActivityAsync(activityId.Value, "Failed", null, ex.Message);
            }
            
            await SendUpdate(purgeId, "failed", $"Error: {ex.Message}", 100);
        }
        finally
        {
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => 
            {
                _jobsTracker.RemoveJob(purgeId);
            });
        }
    }

    private async Task SendUpdate(string purgeId, string status, string message, int progress, 
        int? totalPurged = null, double? elapsedSeconds = null)
    {
        try
        {
            await _hubContext.Clients.Group(purgeId).SendAsync("PurgeUpdate", new
            {
                purgeId,
                status,
                message,
                progress,
                totalPurged,
                elapsedSeconds,
                timestamp = DateTime.UtcNow.ToString("HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send SignalR purge update");
        }
    }
}
