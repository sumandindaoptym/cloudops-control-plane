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

    public async Task<PurgeResult> PurgeDlqWithProgressAsync(
        PurgeRequest request, 
        string accessToken, 
        Func<string, int, Task> onProgress,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        int totalPurged = 0;

        try
        {
            var fullyQualifiedNamespace = $"{request.Namespace}.servicebus.windows.net";
            var credential = new AccessTokenCredential(accessToken);

            await onProgress($"Connecting to namespace: {request.Namespace}", 10);

            await using var client = new ServiceBusClient(fullyQualifiedNamespace, credential);

            var receiverOptions = new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            ServiceBusReceiver receiver;
            if (request.EntityType.Equals("queue", StringComparison.OrdinalIgnoreCase))
            {
                receiver = client.CreateReceiver(request.EntityName, receiverOptions);
                await onProgress($"Opening DLQ receiver for queue: {request.EntityName}", 15);
            }
            else if (request.EntityType.Equals("topic", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(request.TopicSubscriptionName))
                {
                    throw new ArgumentException("Topic subscription name is required");
                }
                receiver = client.CreateReceiver(request.EntityName, request.TopicSubscriptionName, receiverOptions);
                await onProgress($"Opening DLQ receiver for topic: {request.EntityName}/{request.TopicSubscriptionName}", 15);
            }
            else
            {
                throw new ArgumentException($"Invalid entity type: {request.EntityType}");
            }

            await using (receiver)
            {
                await onProgress("Starting purge operation...", 20);

                const int batchSize = 100;
                int emptyBatchCount = 0;
                const int maxEmptyBatches = 3;

                while (emptyBatchCount < maxEmptyBatches && !cancellationToken.IsCancellationRequested)
                {
                    var messages = await receiver.ReceiveMessagesAsync(
                        maxMessages: batchSize,
                        maxWaitTime: TimeSpan.FromSeconds(5),
                        cancellationToken: cancellationToken);

                    if (messages == null || messages.Count == 0)
                    {
                        emptyBatchCount++;
                        await onProgress($"No messages received (attempt {emptyBatchCount}/{maxEmptyBatches})", 
                            Math.Min(90, 20 + (emptyBatchCount * 20)));
                        continue;
                    }

                    emptyBatchCount = 0;

                    foreach (var message in messages)
                    {
                        await receiver.CompleteMessageAsync(message, cancellationToken);
                        totalPurged++;
                    }

                    var progress = Math.Min(85, 20 + (totalPurged / 10));
                    await onProgress($"Purged batch of {messages.Count} messages (total: {totalPurged})", progress);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("Purge completed. Total: {TotalPurged} in {Elapsed}s", 
                totalPurged, stopwatch.Elapsed.TotalSeconds);

            return new PurgeResult
            {
                Success = true,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Logs = new List<string>()
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new PurgeResult
            {
                Success = false,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Error = "Operation was cancelled",
                Logs = new List<string>()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error purging DLQ");

            return new PurgeResult
            {
                Success = false,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Error = ex.Message,
                Logs = new List<string>()
            };
        }
    }

    public async Task<PurgeResult> PurgeDlqFastAsync(
        PurgeRequest request, 
        string accessToken, 
        Func<string, int, int, Task> onProgress,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        int totalPurged = 0;
        const int batchSize = 250;
        const int maxConcurrentCompletions = 10;

        try
        {
            var fullyQualifiedNamespace = $"{request.Namespace}.servicebus.windows.net";
            var credential = new AccessTokenCredential(accessToken);

            await onProgress($"Connecting to namespace: {request.Namespace}", 5, 0);

            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp,
                RetryOptions = new ServiceBusRetryOptions
                {
                    MaxRetries = 3,
                    Delay = TimeSpan.FromMilliseconds(500),
                    MaxDelay = TimeSpan.FromSeconds(5),
                    Mode = ServiceBusRetryMode.Exponential
                }
            };

            await using var client = new ServiceBusClient(fullyQualifiedNamespace, credential, clientOptions);

            var receiverOptions = new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                PrefetchCount = batchSize
            };

            ServiceBusReceiver receiver;
            string entityDescription;
            
            if (request.EntityType.Equals("queue", StringComparison.OrdinalIgnoreCase))
            {
                receiver = client.CreateReceiver(request.EntityName, receiverOptions);
                entityDescription = $"queue: {request.EntityName}";
            }
            else if (request.EntityType.Equals("topic", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(request.TopicSubscriptionName))
                {
                    throw new ArgumentException("Topic subscription name is required");
                }
                receiver = client.CreateReceiver(request.EntityName, request.TopicSubscriptionName, receiverOptions);
                entityDescription = $"topic: {request.EntityName}/{request.TopicSubscriptionName}";
            }
            else
            {
                throw new ArgumentException($"Invalid entity type: {request.EntityType}");
            }

            await onProgress($"Opening DLQ receiver for {entityDescription}", 10, 0);

            await using (receiver)
            {
                await onProgress("Starting fast purge (ReceiveAndDelete mode)...", 15, 0);

                int emptyBatchCount = 0;
                const int maxEmptyBatches = 2;
                var lastProgressUpdate = DateTime.UtcNow;
                const double progressUpdateInterval = 0.5;

                while (emptyBatchCount < maxEmptyBatches && !cancellationToken.IsCancellationRequested)
                {
                    var messages = await receiver.ReceiveMessagesAsync(
                        maxMessages: batchSize,
                        maxWaitTime: TimeSpan.FromSeconds(3),
                        cancellationToken: cancellationToken);

                    if (messages == null || messages.Count == 0)
                    {
                        emptyBatchCount++;
                        await onProgress($"No messages received (attempt {emptyBatchCount}/{maxEmptyBatches})", 
                            Math.Min(95, 20 + (totalPurged / 50)), totalPurged);
                        continue;
                    }

                    emptyBatchCount = 0;
                    totalPurged += messages.Count;

                    var now = DateTime.UtcNow;
                    if ((now - lastProgressUpdate).TotalSeconds >= progressUpdateInterval || messages.Count >= 100)
                    {
                        var rate = totalPurged / Math.Max(1, stopwatch.Elapsed.TotalSeconds);
                        var progress = Math.Min(90, 20 + (totalPurged / 100));
                        await onProgress($"Purged {messages.Count} messages (total: {totalPurged}, rate: {rate:F0}/s)", 
                            progress, totalPurged);
                        lastProgressUpdate = now;
                    }
                }
            }

            stopwatch.Stop();
            var finalRate = totalPurged / Math.Max(1, stopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation("Fast purge completed. Total: {TotalPurged} in {Elapsed:F1}s ({Rate:F0}/s)", 
                totalPurged, stopwatch.Elapsed.TotalSeconds, finalRate);

            return new PurgeResult
            {
                Success = true,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Logs = new List<string>()
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new PurgeResult
            {
                Success = false,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Error = "Operation was cancelled",
                Logs = new List<string>()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during fast purge");

            return new PurgeResult
            {
                Success = false,
                TotalPurged = totalPurged,
                ElapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                Error = ex.Message,
                Logs = new List<string>()
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
