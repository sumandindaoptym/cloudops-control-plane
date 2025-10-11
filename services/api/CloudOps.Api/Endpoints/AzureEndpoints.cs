using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.AspNetCore.Mvc;
using CloudOps.Api.Auth;

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
}

public record SubscriptionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SubscriptionId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}
