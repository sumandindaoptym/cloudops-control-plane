using System.Net.Http.Headers;
using System.Text.Json;
using CloudOps.Web.Models;

namespace CloudOps.Web.Services;

public class AzureSubscriptionService : IAzureSubscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureSubscriptionService> _logger;

    public AzureSubscriptionService(HttpClient httpClient, ILogger<AzureSubscriptionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<AzureSubscription>> GetSubscriptionsAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://management.azure.com/subscriptions?api-version=2020-01-01");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to fetch Azure subscriptions. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Azure API returned {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var subscriptionsResponse = JsonSerializer.Deserialize<AzureSubscriptionsResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (subscriptionsResponse?.Value == null)
        {
            _logger.LogWarning("Azure API returned null or empty subscription list");
            return new List<AzureSubscription>();
        }

        _logger.LogInformation("Successfully retrieved {Count} Azure subscriptions", subscriptionsResponse.Value.Count);
        
        return subscriptionsResponse.Value.Select(s => new AzureSubscription
        {
            SubscriptionId = s.SubscriptionId,
            DisplayName = s.DisplayName,
            State = s.State,
            TenantId = s.TenantId
        }).ToList();
    }
}
