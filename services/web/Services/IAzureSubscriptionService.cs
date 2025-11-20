using CloudOps.Web.Models;

namespace CloudOps.Web.Services;

public interface IAzureSubscriptionService
{
    Task<List<AzureSubscription>> GetSubscriptionsAsync(string accessToken);
}
