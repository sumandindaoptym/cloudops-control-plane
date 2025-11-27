using System.Text.Json.Serialization;

namespace CloudOps.Agent.Models;

public class Job
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? TargetPoolId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public int Priority { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class JobClaimRequest
{
    public Guid AgentId { get; set; }
}

public class JobProgressUpdate
{
    public int ProgressPercent { get; set; }
    public string? CurrentStep { get; set; }
    public string? LogMessage { get; set; }
}

public class JobCompletionUpdate
{
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? Artifacts { get; set; }
}

public class HeartbeatRequest
{
    public int CurrentRunningJobs { get; set; }
    public string? Status { get; set; }
    public AgentInfo? SystemInfo { get; set; }
}

public class HeartbeatResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class AgentRegistrationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
