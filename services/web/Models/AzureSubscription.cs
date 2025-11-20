namespace CloudOps.Web.Models;

public class AzureSubscription
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class AzureSubscriptionsResponse
{
    public List<AzureSubscriptionValue> Value { get; set; } = new();
}

public class AzureSubscriptionValue
{
    public string Id { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
