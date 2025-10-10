namespace CloudOps.Shared.Models;

public class TaskEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<TaskStep> Steps { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class TaskStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Logs { get; set; } = new();
    public int PercentComplete { get; set; }
}

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Environment> Environments { get; set; } = new();
    public string? TeamsWebhookUrl { get; set; }
    public List<string> EmailRecipients { get; set; } = new();
}

public class Environment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "dev";
    public int? TtlMinutes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
