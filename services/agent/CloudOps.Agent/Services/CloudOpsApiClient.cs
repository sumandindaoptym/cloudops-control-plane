using System.Net.Http.Json;
using System.Text.Json;
using CloudOps.Agent.Configuration;
using CloudOps.Agent.Models;
using Microsoft.Extensions.Options;

namespace CloudOps.Agent.Services;

public interface ICloudOpsApiClient
{
    Task<Guid?> RegisterAgentAsync(AgentInfo agentInfo, CancellationToken cancellationToken = default);
    Task<bool> SendHeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default);
    Task<List<Job>> GetAvailableJobsAsync(CancellationToken cancellationToken = default);
    Task<Job?> ClaimJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<bool> UpdateJobProgressAsync(Guid jobId, JobProgressUpdate update, CancellationToken cancellationToken = default);
    Task<bool> CompleteJobAsync(Guid jobId, JobCompletionUpdate update, CancellationToken cancellationToken = default);
}

public class CloudOpsApiClient : ICloudOpsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AgentOptions _options;
    private readonly ILogger<CloudOpsApiClient> _logger;
    private Guid? _agentId;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CloudOpsApiClient(HttpClient httpClient, IOptions<AgentOptions> options, ILogger<CloudOpsApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.ApiUrl))
            throw new ArgumentException("ApiUrl is required");
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new ArgumentException("ApiKey is required");
            
        _httpClient.BaseAddress = new Uri(_options.ApiUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Add("X-Agent-ApiKey", _options.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public Guid? AgentId => _agentId;

    public async Task<Guid?> RegisterAgentAsync(AgentInfo agentInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                name = _options.AgentName,
                poolId = _options.PoolId,
                maxParallelJobs = _options.MaxParallelJobs,
                machineName = agentInfo.MachineName,
                operatingSystem = agentInfo.OperatingSystem,
                architecture = agentInfo.Architecture,
                agentVersion = agentInfo.AgentVersion,
                logicalCores = agentInfo.LogicalCores,
                memoryGb = agentInfo.MemoryGb,
                diskSpaceGb = agentInfo.DiskSpaceGb
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/agents/register", request, JsonOptions, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AgentRegistrationResponse>(JsonOptions, cancellationToken);
                if (result != null)
                {
                    _agentId = result.Id;
                    _logger.LogInformation("Agent registered successfully with ID: {AgentId}", _agentId);
                    return _agentId;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                     response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Authentication failed: {StatusCode} - {Error}. Check your API key.", response.StatusCode, error);
                throw new UnauthorizedAccessException($"API key is invalid or expired: {response.StatusCode}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to register agent: {StatusCode} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent");
        }
        
        return null;
    }

    public async Task<bool> SendHeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        if (_agentId == null)
        {
            _logger.LogWarning("Cannot send heartbeat - agent not registered");
            return false;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/agents/{_agentId}/heartbeat", request, JsonOptions, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Heartbeat sent successfully");
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                     response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError("Heartbeat authentication failed - API key may be invalid or expired");
                throw new UnauthorizedAccessException("API key is invalid or expired");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Heartbeat failed: {StatusCode} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat");
        }
        
        return false;
    }

    public async Task<List<Job>> GetAvailableJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var poolFilter = _options.PoolId != Guid.Empty ? $"?poolId={_options.PoolId}" : "";
            var response = await _httpClient.GetAsync($"api/jobs/available{poolFilter}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var jobs = await response.Content.ReadFromJsonAsync<List<Job>>(JsonOptions, cancellationToken);
                return jobs ?? new List<Job>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available jobs");
        }
        
        return new List<Job>();
    }

    public async Task<Job?> ClaimJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (_agentId == null)
        {
            _logger.LogWarning("Cannot claim job - agent not registered");
            return null;
        }

        try
        {
            var request = new JobClaimRequest { AgentId = _agentId.Value };
            var response = await _httpClient.PostAsJsonAsync($"api/jobs/{jobId}/claim", request, JsonOptions, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var job = await response.Content.ReadFromJsonAsync<Job>(JsonOptions, cancellationToken);
                _logger.LogInformation("Successfully claimed job: {JobId} - {JobName}", jobId, job?.Name);
                return job;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to claim job {JobId}: {StatusCode} - {Error}", jobId, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error claiming job {JobId}", jobId);
        }
        
        return null;
    }

    public async Task<bool> UpdateJobProgressAsync(Guid jobId, JobProgressUpdate update, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/jobs/{jobId}/progress", update, JsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job progress for {JobId}", jobId);
            return false;
        }
    }

    public async Task<bool> CompleteJobAsync(Guid jobId, JobCompletionUpdate update, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/jobs/{jobId}/complete", update, JsonOptions, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Job {JobId} completed successfully", jobId);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to complete job {JobId}: {StatusCode} - {Error}", jobId, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing job {JobId}", jobId);
        }
        
        return false;
    }
}
