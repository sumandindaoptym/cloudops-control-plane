using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using CloudOps.Api.Models;

namespace CloudOps.Api.Services;

public class ServiceBusResourceService : IServiceBusResourceService
{
    private readonly ILogger<ServiceBusResourceService> _logger;

    public ServiceBusResourceService(ILogger<ServiceBusResourceService> logger)
    {
        _logger = logger;
    }

    private ArmClient CreateArmClient(string accessToken)
    {
        var credential = new AccessTokenCredential(accessToken);
        return new ArmClient(credential);
    }

    public async Task<List<ServiceBusNamespaceInfo>> GetNamespacesAsync(string subscriptionId, string accessToken)
    {
        try
        {
            var client = CreateArmClient(accessToken);
            var subscription = client.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));
            
            var namespaces = new List<ServiceBusNamespaceInfo>();
            
            await foreach (var ns in subscription.GetServiceBusNamespacesAsync())
            {
                namespaces.Add(new ServiceBusNamespaceInfo
                {
                    Name = ns.Data.Name,
                    ResourceGroup = ExtractResourceGroup(ns.Id.ToString()),
                    Location = ns.Data.Location.ToString(),
                    Sku = ns.Data.Sku?.Name.ToString() ?? "Unknown",
                    Status = ns.Data.Status?.ToString() ?? "Unknown"
                });
            }

            _logger.LogInformation("Retrieved {Count} Service Bus namespaces for subscription {SubscriptionId}", 
                namespaces.Count, subscriptionId);
            
            return namespaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Service Bus namespaces for subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<List<ServiceBusQueueInfo>> GetQueuesAsync(string subscriptionId, string resourceGroup, string namespaceName, string accessToken)
    {
        try
        {
            var client = CreateArmClient(accessToken);
            var nsId = new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}");
            var ns = client.GetServiceBusNamespaceResource(nsId);
            
            var queues = new List<ServiceBusQueueInfo>();
            
            await foreach (var queue in ns.GetServiceBusQueues())
            {
                queues.Add(new ServiceBusQueueInfo
                {
                    Name = queue.Data.Name,
                    MessageCount = queue.Data.MessageCount ?? 0,
                    DeadLetterMessageCount = 0
                });
            }

            _logger.LogInformation("Retrieved {Count} queues for namespace {Namespace}", queues.Count, namespaceName);
            
            return queues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching queues for namespace {Namespace}", namespaceName);
            throw;
        }
    }

    public async Task<List<ServiceBusTopicInfo>> GetTopicsAsync(string subscriptionId, string resourceGroup, string namespaceName, string accessToken)
    {
        try
        {
            var client = CreateArmClient(accessToken);
            var nsId = new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}");
            var ns = client.GetServiceBusNamespaceResource(nsId);
            
            var topics = new List<ServiceBusTopicInfo>();
            
            await foreach (var topic in ns.GetServiceBusTopics())
            {
                topics.Add(new ServiceBusTopicInfo
                {
                    Name = topic.Data.Name
                });
            }

            _logger.LogInformation("Retrieved {Count} topics for namespace {Namespace}", topics.Count, namespaceName);
            
            return topics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching topics for namespace {Namespace}", namespaceName);
            throw;
        }
    }

    public async Task<List<ServiceBusSubscriptionInfo>> GetSubscriptionsAsync(string subscriptionId, string resourceGroup, string namespaceName, string topicName, string accessToken)
    {
        try
        {
            var client = CreateArmClient(accessToken);
            var topicId = new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}");
            var topic = client.GetServiceBusTopicResource(topicId);
            
            var subscriptions = new List<ServiceBusSubscriptionInfo>();
            
            await foreach (var sub in topic.GetServiceBusSubscriptions())
            {
                subscriptions.Add(new ServiceBusSubscriptionInfo
                {
                    Name = sub.Data.Name,
                    MessageCount = sub.Data.MessageCount ?? 0,
                    DeadLetterMessageCount = 0
                });
            }

            _logger.LogInformation("Retrieved {Count} subscriptions for topic {Topic}", subscriptions.Count, topicName);
            
            return subscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscriptions for topic {Topic}", topicName);
            throw;
        }
    }

    private string ExtractResourceGroup(string resourceId)
    {
        var parts = resourceId.Split('/');
        var rgIndex = Array.IndexOf(parts, "resourceGroups");
        return rgIndex >= 0 && rgIndex < parts.Length - 1 ? parts[rgIndex + 1] : "Unknown";
    }

    private class AccessTokenCredential : TokenCredential
    {
        private readonly string _accessToken;

        public AccessTokenCredential(string accessToken)
        {
            _accessToken = accessToken;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}
