using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using CloudOps.Web.Hubs;
using CloudOps.Web.Models;

namespace CloudOps.Web.Services;

public class PurgeJob
{
    public string PurgeId { get; set; } = string.Empty;
    public PurgeRequest Request { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
}

public interface IPurgeQueue
{
    ValueTask QueuePurgeAsync(PurgeJob job);
    ValueTask<PurgeJob> DequeueAsync(CancellationToken cancellationToken);
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
}

public class PurgeBackgroundService : BackgroundService
{
    private readonly IPurgeQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PurgeHub> _hubContext;
    private readonly ILogger<PurgeBackgroundService> _logger;

    public PurgeBackgroundService(
        IPurgeQueue queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<PurgeHub> hubContext,
        ILogger<PurgeBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Purge Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                await ProcessPurgeJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing purge job");
            }
        }

        _logger.LogInformation("Purge Background Service stopped");
    }

    private async Task ProcessPurgeJobAsync(PurgeJob job, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var purgeId = job.PurgeId;

        try
        {
            await SendUpdate(purgeId, "started", "Connecting to Service Bus...", 5);

            using var scope = _scopeFactory.CreateScope();
            var runtimeService = scope.ServiceProvider.GetRequiredService<IServiceBusRuntimeService>();

            var result = await runtimeService.PurgeDlqWithProgressAsync(
                job.Request,
                job.AccessToken,
                async (log, progress) =>
                {
                    await SendUpdate(purgeId, "progress", log, progress);
                },
                cancellationToken);

            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

            if (result.Success)
            {
                await SendUpdate(purgeId, "completed", 
                    $"Purge completed. Total messages purged: {result.TotalPurged}", 100,
                    result.TotalPurged, elapsed);
            }
            else
            {
                await SendUpdate(purgeId, "failed", result.Error ?? "Unknown error", 100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in purge job {PurgeId}", purgeId);
            await SendUpdate(purgeId, "failed", $"Error: {ex.Message}", 100);
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
