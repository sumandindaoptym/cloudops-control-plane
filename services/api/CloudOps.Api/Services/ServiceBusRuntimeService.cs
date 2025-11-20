using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using CloudOps.Api.Models;

namespace CloudOps.Api.Services;

public class ServiceBusRuntimeService : IServiceBusRuntimeService
{
    private readonly ILogger<ServiceBusRuntimeService> _logger;

    public ServiceBusRuntimeService(ILogger<ServiceBusRuntimeService> logger)
    {
        _logger = logger;
    }

    public async Task<DlqCountResponse> GetDlqCountAsync(DlqCountRequest request, string accessToken)
    {
        try
        {
            var fullyQualifiedNamespace = $"{request.Namespace}.servicebus.windows.net";
            var credential = new AccessTokenCredential(accessToken);
            var adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, credential);

            long deadLetterCount = 0;
            long activeCount = 0;

            if (request.EntityType.Equals("queue", StringComparison.OrdinalIgnoreCase))
            {
                var runtimeProps = await adminClient.GetQueueRuntimePropertiesAsync(request.EntityName);
                deadLetterCount = runtimeProps.Value.DeadLetterMessageCount;
                activeCount = runtimeProps.Value.ActiveMessageCount;
                
                _logger.LogInformation("Queue {Queue}: {ActiveCount} active, {DlqCount} DLQ messages", 
                    request.EntityName, activeCount, deadLetterCount);
            }
            else if (request.EntityType.Equals("topic", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(request.TopicSubscriptionName))
                {
                    throw new ArgumentException("Topic subscription name is required for topic entities");
                }

                var runtimeProps = await adminClient.GetSubscriptionRuntimePropertiesAsync(
                    request.EntityName, 
                    request.TopicSubscriptionName);
                deadLetterCount = runtimeProps.Value.DeadLetterMessageCount;
                activeCount = runtimeProps.Value.ActiveMessageCount;
                
                _logger.LogInformation("Topic {Topic}/Subscription {Subscription}: {ActiveCount} active, {DlqCount} DLQ messages", 
                    request.EntityName, request.TopicSubscriptionName, activeCount, deadLetterCount);
            }
            else
            {
                throw new ArgumentException($"Invalid entity type: {request.EntityType}");
            }

            return new DlqCountResponse
            {
                DeadLetterCount = deadLetterCount,
                ActiveCount = activeCount,
                EntityType = request.EntityType,
                EntityName = request.EntityName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DLQ count for {EntityType} {EntityName}", 
                request.EntityType, request.EntityName);
            throw;
        }
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
