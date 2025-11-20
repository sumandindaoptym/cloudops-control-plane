using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.AspNetCore.Mvc;
using CloudOps.Api.Auth;
using CloudOps.Api.Services;
using CloudOps.Api.Models;

namespace CloudOps.Api.Endpoints;

public static class AzureEndpoints
{
    public static void MapAzureEndpoints(this WebApplication app)
    {
        var azure = app.MapGroup("/api/azure").WithTags("Azure");

        azure.MapGet("/subscriptions", GetSubscriptions)
            .WithName("GetAzureSubscriptions")
            .WithDescription("Get all Azure subscriptions the user has access to")
            .Produces<List<SubscriptionDto>>(200);

        var serviceBus = azure.MapGroup("/servicebus").WithTags("Service Bus");

        serviceBus.MapGet("/namespaces", GetNamespaces)
            .WithName("GetServiceBusNamespaces")
            .WithDescription("Get all Service Bus namespaces in a subscription");

        serviceBus.MapGet("/queues", GetQueues)
            .WithName("GetServiceBusQueues")
            .WithDescription("Get all queues in a Service Bus namespace");

        serviceBus.MapGet("/topics", GetTopics)
            .WithName("GetServiceBusTopics")
            .WithDescription("Get all topics in a Service Bus namespace");

        serviceBus.MapGet("/topics/{topic}/subscriptions", GetTopicSubscriptions)
            .WithName("GetServiceBusSubscriptions")
            .WithDescription("Get all subscriptions for a Service Bus topic");

        serviceBus.MapPost("/dlq/count", GetDlqCount)
            .WithName("GetDlqCount")
            .WithDescription("Get dead letter queue message count");
    }

    private static async Task<IResult> GetSubscriptions(
        HttpContext httpContext,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        try
        {
            // Get user's access token from Authorization header
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                logger.LogError("No bearer token provided");
                return Results.Unauthorized();
            }

            var userAccessToken = authHeader.Substring("Bearer ".Length).Trim();
            
            // Create credential using user's access token
            var credential = new AccessTokenCredential(userAccessToken);
            
            // Create ARM client with user's credentials
            var armClient = new ArmClient(credential);
            
            var tenantId = configuration["AZURE_AD_TENANT_ID"];

            // Get all subscriptions
            var subscriptions = new List<SubscriptionDto>();
            
            await foreach (var subscription in armClient.GetSubscriptions())
            {
                subscriptions.Add(new SubscriptionDto
                {
                    Id = subscription.Id.ToString(),
                    Name = subscription.Data.DisplayName,
                    SubscriptionId = subscription.Data.SubscriptionId.ToString(),
                    TenantId = subscription.Data.TenantId?.ToString() ?? tenantId ?? "unknown",
                    State = subscription.Data.State?.ToString() ?? "Unknown"
                });
            }

            logger.LogInformation("Retrieved {Count} Azure subscriptions for user", subscriptions.Count);
            return Results.Ok(subscriptions);
        }
        catch (RequestFailedException ex) when (ex.Status == 401)
        {
            // Token is invalid, expired, or has wrong audience - return 401 so frontend can sign out
            logger.LogError(ex, "Unauthorized: Invalid or expired token");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Azure subscriptions");
            return Results.Problem($"Failed to fetch subscriptions: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> GetNamespaces(
        [FromQuery] string subscriptionId,
        HttpContext httpContext,
        IServiceBusResourceService serviceBusService,
        ILogger<Program> logger)
    {
        try
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Results.Unauthorized();
            }

            var userAccessToken = authHeader.Substring("Bearer ".Length).Trim();
            var namespaces = await serviceBusService.GetNamespacesAsync(subscriptionId, userAccessToken);
            
            logger.LogInformation("Retrieved {Count} Service Bus namespaces", namespaces.Count);
            return Results.Ok(namespaces);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Service Bus namespaces");
            return Results.Problem($"Failed to fetch namespaces: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> GetQueues(
        [FromQuery] string subscriptionId,
        [FromQuery] string resourceGroup,
        [FromQuery] string namespaceName,
        HttpContext httpContext,
        IServiceBusResourceService serviceBusService,
        ILogger<Program> logger)
    {
        try
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Results.Unauthorized();
            }

            var userAccessToken = authHeader.Substring("Bearer ".Length).Trim();
            var queues = await serviceBusService.GetQueuesAsync(subscriptionId, resourceGroup, namespaceName, userAccessToken);
            
            logger.LogInformation("Retrieved {Count} queues", queues.Count);
            return Results.Ok(queues);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Service Bus queues");
            return Results.Problem($"Failed to fetch queues: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> GetTopics(
        [FromQuery] string subscriptionId,
        [FromQuery] string resourceGroup,
        [FromQuery] string namespaceName,
        HttpContext httpContext,
        IServiceBusResourceService serviceBusService,
        ILogger<Program> logger)
    {
        try
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Results.Unauthorized();
            }

            var userAccessToken = authHeader.Substring("Bearer ".Length).Trim();
            var topics = await serviceBusService.GetTopicsAsync(subscriptionId, resourceGroup, namespaceName, userAccessToken);
            
            logger.LogInformation("Retrieved {Count} topics", topics.Count);
            return Results.Ok(topics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Service Bus topics");
            return Results.Problem($"Failed to fetch topics: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> GetTopicSubscriptions(
        string topic,
        [FromQuery] string subscriptionId,
        [FromQuery] string resourceGroup,
        [FromQuery] string namespaceName,
        HttpContext httpContext,
        IServiceBusResourceService serviceBusService,
        ILogger<Program> logger)
    {
        try
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Results.Unauthorized();
            }

            var userAccessToken = authHeader.Substring("Bearer ".Length).Trim();
            var subscriptions = await serviceBusService.GetSubscriptionsAsync(subscriptionId, resourceGroup, namespaceName, topic, userAccessToken);
            
            logger.LogInformation("Retrieved {Count} subscriptions for topic {Topic}", subscriptions.Count, topic);
            return Results.Ok(subscriptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Service Bus topic subscriptions");
            return Results.Problem($"Failed to fetch topic subscriptions: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> GetDlqCount(
        [FromBody] DlqCountRequest request,
        HttpContext httpContext,
        IServiceBusRuntimeService runtimeService,
        ILogger<Program> logger)
    {
        try
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Results.Unauthorized();
            }

            var userAccessToken = authHeader.Substring("Bearer ".Length).Trim();
            var dlqCount = await runtimeService.GetDlqCountAsync(request, userAccessToken);
            
            logger.LogInformation("DLQ count retrieved: {Count}", dlqCount.DeadLetterCount);
            return Results.Ok(dlqCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get DLQ count");
            return Results.Problem($"Failed to get DLQ count: {ex.Message}", statusCode: 500);
        }
    }
}

public record SubscriptionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SubscriptionId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}
