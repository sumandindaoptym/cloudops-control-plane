namespace CloudOps.Shared.DTOs;

public record TaskMessage
{
    public string Type { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public Guid TaskId { get; init; }
    public string? IdempotencyKey { get; init; }
    public Dictionary<string, object> Payload { get; init; } = new();
}

public record TaskUpdate
{
    public Guid TaskId { get; init; }
    public string? StepName { get; init; }
    public string? Status { get; init; }
    public string? Message { get; init; }
    public int? PercentComplete { get; init; }
}

public record DeploymentRequest
{
    public Guid ProjectId { get; init; }
    public Guid EnvId { get; init; }
    public string TemplateId { get; init; } = string.Empty;
    public Dictionary<string, string> Parameters { get; init; } = new();
}

public record BackupRequest
{
    public string Engine { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
}

public record RestoreRequest
{
    public string Engine { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public string ArtifactId { get; init; } = string.Empty;
    public string TargetDatabase { get; init; } = string.Empty;
}

public record RestartPodsRequest
{
    public string Namespace { get; init; } = string.Empty;
    public string WorkloadName { get; init; } = string.Empty;
}

public record SandboxRequest
{
    public Guid ProjectId { get; init; }
    public int TtlMinutes { get; init; } = 60;
}
