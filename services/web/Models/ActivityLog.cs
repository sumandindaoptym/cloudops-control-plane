namespace CloudOps.Web.Models;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string? SubResourceName { get; set; }
    public string Status { get; set; } = "Running";
    public int? ItemsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
}
