using CloudOps.Agent.Services;

namespace CloudOps.Agent.Handlers;

public interface IJobHandler
{
    string JobType { get; }
    Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken);
}
