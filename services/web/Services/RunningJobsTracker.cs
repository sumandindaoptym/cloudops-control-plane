using System.Collections.Concurrent;

namespace CloudOps.Web.Services;

public class RunningPurgeJob
{
    public string PurgeId { get; set; } = string.Empty;
    public Guid? ActivityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string NamespaceName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? TopicSubscriptionName { get; set; }
    public string Status { get; set; } = "Starting";
    public int Progress { get; set; }
    public int TotalPurged { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public ConcurrentQueue<LogEntry> Logs { get; set; } = new();
}

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "Info";
}

public interface IRunningJobsTracker
{
    void AddJob(RunningPurgeJob job);
    void UpdateJob(string purgeId, Action<RunningPurgeJob> update);
    void RemoveJob(string purgeId);
    RunningPurgeJob? GetJob(string purgeId);
    IEnumerable<RunningPurgeJob> GetJobsForUser(string userId);
    IEnumerable<RunningPurgeJob> GetAllRunningJobs();
    void AddLog(string purgeId, string message, string level = "Info");
    IEnumerable<LogEntry> GetLogs(string purgeId, int skip = 0);
}

public class RunningJobsTracker : IRunningJobsTracker
{
    private readonly ConcurrentDictionary<string, RunningPurgeJob> _jobs = new();
    private const int MaxLogsPerJob = 1000;

    public void AddJob(RunningPurgeJob job)
    {
        _jobs.TryAdd(job.PurgeId, job);
    }

    public void UpdateJob(string purgeId, Action<RunningPurgeJob> update)
    {
        if (_jobs.TryGetValue(purgeId, out var job))
        {
            update(job);
        }
    }

    public void RemoveJob(string purgeId)
    {
        _jobs.TryRemove(purgeId, out _);
    }

    public RunningPurgeJob? GetJob(string purgeId)
    {
        return _jobs.TryGetValue(purgeId, out var job) ? job : null;
    }

    public IEnumerable<RunningPurgeJob> GetJobsForUser(string userId)
    {
        CleanupOldJobs();
        return _jobs.Values
            .Where(j => j.UserId == userId && (j.Status == "Running" || j.Status == "Starting"))
            .OrderByDescending(j => j.StartTime);
    }

    public IEnumerable<RunningPurgeJob> GetAllRunningJobs()
    {
        CleanupOldJobs();
        return _jobs.Values
            .Where(j => j.Status == "Running" || j.Status == "Starting")
            .OrderByDescending(j => j.StartTime);
    }
    
    private void CleanupOldJobs()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);
        var oldJobs = _jobs.Values
            .Where(j => (j.Status == "Completed" || j.Status == "Failed") && j.EndTime < cutoff)
            .Select(j => j.PurgeId)
            .ToList();
            
        foreach (var id in oldJobs)
        {
            _jobs.TryRemove(id, out _);
        }
    }

    public void AddLog(string purgeId, string message, string level = "Info")
    {
        if (_jobs.TryGetValue(purgeId, out var job))
        {
            job.Logs.Enqueue(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = message,
                Level = level
            });

            while (job.Logs.Count > MaxLogsPerJob)
            {
                job.Logs.TryDequeue(out _);
            }
        }
    }

    public IEnumerable<LogEntry> GetLogs(string purgeId, int skip = 0)
    {
        if (_jobs.TryGetValue(purgeId, out var job))
        {
            return job.Logs.Skip(skip).ToList();
        }
        return Enumerable.Empty<LogEntry>();
    }
}
