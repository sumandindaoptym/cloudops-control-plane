using System.Collections.Concurrent;
using CloudOps.Agent.Configuration;
using CloudOps.Agent.Handlers;
using CloudOps.Agent.Models;
using Microsoft.Extensions.Options;

namespace CloudOps.Agent.Services;

public class JobExecutionService
{
    private readonly ICloudOpsApiClient _apiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentOptions _options;
    private readonly ILogger<JobExecutionService> _logger;
    private readonly ConcurrentDictionary<Guid, Task> _runningJobs = new();

    public JobExecutionService(
        ICloudOpsApiClient apiClient,
        IServiceProvider serviceProvider,
        IOptions<AgentOptions> options,
        ILogger<JobExecutionService> logger)
    {
        _apiClient = apiClient;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public int RunningJobCount => _runningJobs.Count;
    public bool CanAcceptMoreJobs => _runningJobs.Count < _options.MaxParallelJobs;

    public async Task ExecuteJobAsync(Job job, CancellationToken cancellationToken)
    {
        if (_runningJobs.ContainsKey(job.Id))
        {
            _logger.LogWarning("Job {JobId} is already running", job.Id);
            return;
        }

        var executionTask = ExecuteJobInternalAsync(job, cancellationToken);
        _runningJobs.TryAdd(job.Id, executionTask);

        try
        {
            await executionTask;
        }
        finally
        {
            _runningJobs.TryRemove(job.Id, out _);
        }
    }

    private async Task ExecuteJobInternalAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting execution of job {JobId}: {JobName} ({JobType})", 
            job.Id, job.Name, job.Type);

        var handler = GetHandler(job.Type);
        if (handler == null)
        {
            _logger.LogError("No handler found for job type: {JobType}", job.Type);
            await _apiClient.CompleteJobAsync(job.Id, new JobCompletionUpdate
            {
                Success = false,
                ErrorMessage = $"No handler found for job type: {job.Type}"
            }, cancellationToken);
            return;
        }

        try
        {
            await _apiClient.UpdateJobProgressAsync(job.Id, new JobProgressUpdate
            {
                ProgressPercent = 0,
                CurrentStep = "Starting job execution",
                LogMessage = $"Job {job.Name} started on agent {_options.AgentName}"
            }, cancellationToken);

            var context = new JobExecutionContext
            {
                Job = job,
                WorkingDirectory = Path.Combine(_options.WorkingDirectory, job.Id.ToString()),
                ProgressCallback = async (percent, step, log) =>
                {
                    await _apiClient.UpdateJobProgressAsync(job.Id, new JobProgressUpdate
                    {
                        ProgressPercent = percent,
                        CurrentStep = step,
                        LogMessage = log
                    }, cancellationToken);
                }
            };

            Directory.CreateDirectory(context.WorkingDirectory);

            var result = await handler.ExecuteAsync(context, cancellationToken);

            await _apiClient.CompleteJobAsync(job.Id, new JobCompletionUpdate
            {
                Success = result.Success,
                Result = result.Result,
                ErrorMessage = result.ErrorMessage,
                Artifacts = result.Artifacts
            }, cancellationToken);

            _logger.LogInformation("Job {JobId} completed with success: {Success}", job.Id, result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobId}", job.Id);
            
            await _apiClient.CompleteJobAsync(job.Id, new JobCompletionUpdate
            {
                Success = false,
                ErrorMessage = $"Job execution failed: {ex.Message}"
            }, cancellationToken);
        }
    }

    private IJobHandler? GetHandler(string jobType)
    {
        return jobType.ToLowerInvariant() switch
        {
            "database-backup" or "db-backup" => _serviceProvider.GetService<DatabaseBackupHandler>(),
            "database-restore" or "db-restore" => _serviceProvider.GetService<DatabaseRestoreHandler>(),
            "script" or "powershell" or "bash" => _serviceProvider.GetService<ScriptExecutionHandler>(),
            "restart-pods" or "pod-restart" => _serviceProvider.GetService<PodRestartHandler>(),
            _ => _serviceProvider.GetService<GenericJobHandler>()
        };
    }
}

public class JobExecutionContext
{
    public Job Job { get; set; } = null!;
    public string WorkingDirectory { get; set; } = string.Empty;
    public Func<int, string?, string?, Task> ProgressCallback { get; set; } = null!;
}

public class JobExecutionResult
{
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? Artifacts { get; set; }
}
