using CloudOps.Agent.Configuration;
using CloudOps.Agent.Models;
using Microsoft.Extensions.Options;

namespace CloudOps.Agent.Services;

public class HeartbeatService : BackgroundService
{
    private readonly ICloudOpsApiClient _apiClient;
    private readonly JobExecutionService _jobExecutionService;
    private readonly AgentOptions _options;
    private readonly ILogger<HeartbeatService> _logger;

    public HeartbeatService(
        ICloudOpsApiClient apiClient,
        JobExecutionService jobExecutionService,
        IOptions<AgentOptions> options,
        ILogger<HeartbeatService> logger)
    {
        _apiClient = apiClient;
        _jobExecutionService = jobExecutionService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Heartbeat service started with interval: {Interval}s", _options.HeartbeatIntervalSeconds);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new HeartbeatRequest
                {
                    CurrentRunningJobs = _jobExecutionService.RunningJobCount,
                    Status = _jobExecutionService.RunningJobCount > 0 ? "Busy" : "Online",
                    SystemInfo = AgentInfo.Collect()
                };
                
                await _apiClient.SendHeartbeatAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat service");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds), stoppingToken);
        }
    }
}
