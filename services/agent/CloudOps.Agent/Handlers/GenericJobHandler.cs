using CloudOps.Agent.Services;

namespace CloudOps.Agent.Handlers;

public class GenericJobHandler : IJobHandler
{
    private readonly ILogger<GenericJobHandler> _logger;

    public GenericJobHandler(ILogger<GenericJobHandler> logger)
    {
        _logger = logger;
    }

    public string JobType => "generic";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing generic job: {JobName} ({JobType})", 
            context.Job.Name, context.Job.Type);
        
        await context.ProgressCallback(10, "Starting", "Generic job handler started...");
        
        await Task.Delay(1000, cancellationToken);
        
        await context.ProgressCallback(50, "Processing", $"Processing job type: {context.Job.Type}");
        
        await Task.Delay(1000, cancellationToken);
        
        await context.ProgressCallback(100, "Complete", "Job completed");

        return new JobExecutionResult
        {
            Success = true,
            Result = $"Generic job '{context.Job.Name}' of type '{context.Job.Type}' executed successfully. " +
                     $"Parameters: {System.Text.Json.JsonSerializer.Serialize(context.Job.Parameters)}"
        };
    }
}
