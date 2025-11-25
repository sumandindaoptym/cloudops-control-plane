using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CloudOps.Web.Models;

namespace CloudOps.Web.Services;

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

    public async Task<PurgeResult> PurgeDlqAsync(PurgeRequest request, string accessToken, Action<PurgeProgress>? onProgress = null)
    {
        var logs = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        var taskId = Guid.NewGuid().ToString("N")[..8];
        int totalPurged = 0;

        void AddLog(string message)
        {
            var logEntry = $"[{DateTime.UtcNow:HH:mm:ss}] {message}";
            logs.Add(logEntry);
            _logger.LogInformation(message);
        }

        void ReportProgress(string status, string? error = null)
        {
            onProgress?.Invoke(new PurgeProgress
            {
                TaskId = taskId,
                Status = status,
                TotalReceived = totalPurged,
                TotalCompleted = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Error = error,
                Logs = new List<string>(logs)
            });
        }

        try
        {
            var fullyQualifiedNamespace = $"{request.Namespace}.servicebus.windows.net";
            var credential = new AccessTokenCredential(accessToken);

            AddLog($"Connecting to Service Bus namespace: {request.Namespace}");
            ReportProgress("connecting");

            await using var client = new ServiceBusClient(fullyQualifiedNamespace, credential);

            string dlqPath;
            if (request.EntityType.Equals("queue", StringComparison.OrdinalIgnoreCase))
            {
                dlqPath = $"{request.EntityName}/$DeadLetterQueue";
                AddLog($"Opening DLQ receiver for queue: {request.EntityName}");
            }
            else if (request.EntityType.Equals("topic", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(request.TopicSubscriptionName))
                {
                    throw new ArgumentException("Topic subscription name is required for topic entities");
                }
                dlqPath = $"{request.EntityName}/Subscriptions/{request.TopicSubscriptionName}/$DeadLetterQueue";
                AddLog($"Opening DLQ receiver for topic: {request.EntityName}, subscription: {request.TopicSubscriptionName}");
            }
            else
            {
                throw new ArgumentException($"Invalid entity type: {request.EntityType}");
            }

            var receiverOptions = new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            ServiceBusReceiver receiver;
            if (request.EntityType.Equals("queue", StringComparison.OrdinalIgnoreCase))
            {
                receiver = client.CreateReceiver(request.EntityName, receiverOptions);
            }
            else
            {
                receiver = client.CreateReceiver(request.EntityName, request.TopicSubscriptionName!, receiverOptions);
            }

            await using (receiver)
            {
                AddLog("Starting purge operation...");
                ReportProgress("purging");

                const int batchSize = 100;
                int emptyBatchCount = 0;
                const int maxEmptyBatches = 3;

                while (emptyBatchCount < maxEmptyBatches)
                {
                    var messages = await receiver.ReceiveMessagesAsync(
                        maxMessages: batchSize,
                        maxWaitTime: TimeSpan.FromSeconds(5));

                    if (messages == null || messages.Count == 0)
                    {
                        emptyBatchCount++;
                        AddLog($"No messages received (attempt {emptyBatchCount}/{maxEmptyBatches})");
                        continue;
                    }

                    emptyBatchCount = 0;

                    foreach (var message in messages)
                    {
                        await receiver.CompleteMessageAsync(message);
                        totalPurged++;
                    }

                    AddLog($"Purged batch of {messages.Count} messages (total: {totalPurged})");
                    ReportProgress("purging");
                }

                AddLog($"Purge completed. Total messages purged: {totalPurged}");
                ReportProgress("completed");
            }

            stopwatch.Stop();
            return new PurgeResult
            {
                Success = true,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMessage = $"Error during purge: {ex.Message}";
            AddLog(errorMessage);
            _logger.LogError(ex, "Error purging DLQ for {EntityType} {EntityName}", 
                request.EntityType, request.EntityName);

            ReportProgress("failed", ex.Message);

            return new PurgeResult
            {
                Success = false,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Error = ex.Message,
                Logs = logs
            };
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
