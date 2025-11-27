using CloudOps.Agent.Configuration;
using Microsoft.Extensions.Options;

namespace CloudOps.Agent.Services;

public class JobPollingService : BackgroundService
{
    private readonly ICloudOpsApiClient _apiClient;
    private readonly JobExecutionService _jobExecutionService;
    private readonly AgentOptions _options;
    private readonly ILogger<JobPollingService> _logger;

    public JobPollingService(
        ICloudOpsApiClient apiClient,
        JobExecutionService jobExecutionService,
        IOptions<AgentOptions> options,
        ILogger<JobPollingService> logger)
    {
        _apiClient = apiClient;
        _jobExecutionService = jobExecutionService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job polling service started with interval: {Interval}s", _options.JobPollIntervalSeconds);
        
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_jobExecutionService.CanAcceptMoreJobs)
                {
                    var availableJobs = await _apiClient.GetAvailableJobsAsync(stoppingToken);
                    
                    foreach (var job in availableJobs.OrderByDescending(j => j.Priority))
                    {
                        if (!_jobExecutionService.CanAcceptMoreJobs)
                            break;
                        
                        var claimedJob = await _apiClient.ClaimJobAsync(job.Id, stoppingToken);
                        if (claimedJob != null)
                        {
                            _logger.LogInformation("Claimed job {JobId}: {JobName} ({JobType})", 
                                claimedJob.Id, claimedJob.Name, claimedJob.Type);
                            
                            _ = _jobExecutionService.ExecuteJobAsync(claimedJob, stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job polling service");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(_options.JobPollIntervalSeconds), stoppingToken);
        }
    }
}
