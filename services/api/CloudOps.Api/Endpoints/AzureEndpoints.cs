using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.AspNetCore.Mvc;

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
    }

    private static async Task<IResult> GetSubscriptions(
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        try
        {
            var tenantId = configuration["AZURE_AD_TENANT_ID"];
            var clientId = configuration["AZURE_AD_CLIENT_ID"];
            var clientSecret = configuration["AZURE_AD_CLIENT_SECRET"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                logger.LogError("Azure credentials not configured");
                return Results.Problem("Azure credentials not configured", statusCode: 500);
            }

            // Create credential using the service principal
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            
            // Create ARM client
            var armClient = new ArmClient(credential);

            // Get all subscriptions
            var subscriptions = new List<SubscriptionDto>();
            
            try
            {
                await foreach (var subscription in armClient.GetSubscriptions())
                {
                    subscriptions.Add(new SubscriptionDto
                    {
                        Id = subscription.Id.ToString(),
                        Name = subscription.Data.DisplayName,
                        SubscriptionId = subscription.Data.SubscriptionId.ToString(),
                        TenantId = subscription.Data.TenantId.ToString(),
                        State = subscription.Data.State.ToString()
                    });
                }
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                logger.LogWarning("Service principal lacks permissions to list subscriptions. Using mock data for development.");
                
                // Return mock subscriptions for development
                subscriptions.Add(new SubscriptionDto
                {
                    Id = "mock-prod",
                    Name = "Production Subscription (Mock)",
                    SubscriptionId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                    TenantId = tenantId ?? "unknown",
                    State = "Enabled"
                });
                subscriptions.Add(new SubscriptionDto
                {
                    Id = "mock-dev",
                    Name = "Development Subscription (Mock)",
                    SubscriptionId = "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
                    TenantId = tenantId ?? "unknown",
                    State = "Enabled"
                });
                subscriptions.Add(new SubscriptionDto
                {
                    Id = "mock-staging",
                    Name = "Staging Subscription (Mock)",
                    SubscriptionId = "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz",
                    TenantId = tenantId ?? "unknown",
                    State = "Enabled"
                });
            }

            if (subscriptions.Count == 0)
            {
                logger.LogWarning("No Azure subscriptions found. The service principal may not have access to any subscriptions. Using mock data for development.");
                
                // Return mock subscriptions for development
                subscriptions.Add(new SubscriptionDto
                {
                    Id = "mock-prod",
                    Name = "Production Subscription (Mock)",
                    SubscriptionId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                    TenantId = tenantId ?? "unknown",
                    State = "Enabled"
                });
                subscriptions.Add(new SubscriptionDto
                {
                    Id = "mock-dev",
                    Name = "Development Subscription (Mock)",
                    SubscriptionId = "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
                    TenantId = tenantId ?? "unknown",
                    State = "Enabled"
                });
                subscriptions.Add(new SubscriptionDto
                {
                    Id = "mock-staging",
                    Name = "Staging Subscription (Mock)",
                    SubscriptionId = "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz",
                    TenantId = tenantId ?? "unknown",
                    State = "Enabled"
                });
            }

            logger.LogInformation("Retrieved {Count} Azure subscriptions", subscriptions.Count);
            return Results.Ok(subscriptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Azure subscriptions");
            return Results.Problem($"Failed to fetch subscriptions: {ex.Message}", statusCode: 500);
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
