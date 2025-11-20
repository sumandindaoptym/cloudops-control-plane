namespace CloudOps.Web.Models;

public class ServiceBusNamespaceInfo
{
    public string Name { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ServiceBusQueueInfo
{
    public string Name { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long DeadLetterMessageCount { get; set; }
}

public class ServiceBusTopicInfo
{
    public string Name { get; set; } = string.Empty;
}

public class ServiceBusSubscriptionInfo
{
    public string Name { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long DeadLetterMessageCount { get; set; }
}

public class DlqCountRequest
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? TopicSubscriptionName { get; set; }
}

public class DlqCountResponse
{
    public long DeadLetterCount { get; set; }
    public long ActiveCount { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
}

public class PurgeRequest
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? TopicSubscriptionName { get; set; }
}

public class PurgeProgress
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalReceived { get; set; }
    public int TotalCompleted { get; set; }
    public int ElapsedSeconds { get; set; }
    public string? Error { get; set; }
    public List<string> Logs { get; set; } = new();
}
