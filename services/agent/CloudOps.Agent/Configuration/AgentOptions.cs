namespace CloudOps.Agent.Configuration;

public class AgentOptions
{
    public const string SectionName = "Agent";
    
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Guid PoolId { get; set; }
    public string AgentName { get; set; } = Environment.MachineName;
    public int MaxParallelJobs { get; set; } = 2;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int JobPollIntervalSeconds { get; set; } = 5;
    public string WorkingDirectory { get; set; } = "./work";
    public string LogDirectory { get; set; } = "./logs";
}
