using CloudOps.Web.Models;

namespace CloudOps.Web.Services;

public interface IServiceBusResourceService
{
    Task<List<ServiceBusNamespaceInfo>> GetNamespacesAsync(string subscriptionId, string accessToken);
    Task<List<ServiceBusQueueInfo>> GetQueuesAsync(string subscriptionId, string resourceGroup, string namespaceName, string accessToken);
    Task<List<ServiceBusTopicInfo>> GetTopicsAsync(string subscriptionId, string resourceGroup, string namespaceName, string accessToken);
    Task<List<ServiceBusSubscriptionInfo>> GetSubscriptionsAsync(string subscriptionId, string resourceGroup, string namespaceName, string topicName, string accessToken);
}
