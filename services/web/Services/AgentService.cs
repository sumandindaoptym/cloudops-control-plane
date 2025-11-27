using System.Security.Cryptography;
using CloudOps.Web.Data;
using CloudOps.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudOps.Web.Services;

public interface IAgentService
{
    Task<Agent> CreateAgentAsync(string userId, string userEmail, AgentRegistrationRequest request);
    Task<Agent?> GetAgentByIdAsync(Guid agentId);
    Task<Agent?> GetAgentByApiKeyAsync(string apiKey);
    Task<List<Agent>> GetAgentsByUserAsync(string userId);
    Task<List<Agent>> GetAllAgentsAsync();
    Task<Agent?> UpdateHeartbeatAsync(Guid agentId, AgentHeartbeatRequest request);
    Task<bool> DeleteAgentAsync(Guid agentId);
    
    Task<AgentJob> CreateJobAsync(string userId, string userEmail, CreateJobRequest request);
    Task<List<AgentJob>> GetPendingJobsForAgentAsync(Guid agentId, int limit = 5);
    Task<AgentJob?> ClaimJobAsync(Guid agentId, Guid jobId);
    Task<AgentJob?> UpdateJobProgressAsync(Guid agentId, Guid jobId, JobProgressRequest request);
    Task<AgentJob?> CompleteJobAsync(Guid agentId, Guid jobId, JobCompleteRequest request);
    Task<List<AgentJob>> GetJobsAsync(string? userId = null, int limit = 100);
    Task<AgentJob?> GetJobByIdAsync(Guid jobId);
    Task<bool> CancelJobAsync(Guid jobId);
    
    Task UpdateAgentStatusesAsync();
}

public class AgentService : IAgentService
{
    private readonly ActivityDbContext _context;
    private readonly ILogger<AgentService> _logger;
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromMinutes(2);

    public AgentService(ActivityDbContext context, ILogger<AgentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Agent> CreateAgentAsync(string userId, string userEmail, AgentRegistrationRequest request)
    {
        var apiKey = GenerateApiKey();
        
        var agent = new Agent
        {
            Name = request.Name,
            Description = request.Description,
            MaxParallelJobs = request.MaxParallelJobs > 0 ? request.MaxParallelJobs : 2,
            HostName = request.HostName,
            IpAddress = request.IpAddress,
            OperatingSystem = request.OperatingSystem,
            AgentVersion = request.AgentVersion,
            ApiKey = apiKey,
            Status = "Offline",
            CreatedByUserId = userId,
            CreatedByUserEmail = userEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Agent {AgentName} created by user {UserId}", agent.Name, userId);
        return agent;
    }

    public async Task<Agent?> GetAgentByIdAsync(Guid agentId)
    {
        return await _context.Agents.FindAsync(agentId);
    }

    public async Task<Agent?> GetAgentByApiKeyAsync(string apiKey)
    {
        return await _context.Agents.FirstOrDefaultAsync(a => a.ApiKey == apiKey);
    }

    public async Task<List<Agent>> GetAgentsByUserAsync(string userId)
    {
        return await _context.Agents
            .Where(a => a.CreatedByUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Agent>> GetAllAgentsAsync()
    {
        return await _context.Agents
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Agent?> UpdateHeartbeatAsync(Guid agentId, AgentHeartbeatRequest request)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null) return null;

        agent.LastHeartbeat = DateTime.UtcNow;
        agent.Status = request.Status;
        agent.CurrentRunningJobs = request.CurrentRunningJobs;
        
        if (!string.IsNullOrEmpty(request.HostName))
            agent.HostName = request.HostName;
        if (!string.IsNullOrEmpty(request.IpAddress))
            agent.IpAddress = request.IpAddress;
        if (!string.IsNullOrEmpty(request.OperatingSystem))
            agent.OperatingSystem = request.OperatingSystem;
        if (!string.IsNullOrEmpty(request.AgentVersion))
            agent.AgentVersion = request.AgentVersion;

        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task<bool> DeleteAgentAsync(Guid agentId)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null) return false;

        _context.Agents.Remove(agent);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Agent {AgentId} deleted", agentId);
        return true;
    }

    public async Task<AgentJob> CreateJobAsync(string userId, string userEmail, CreateJobRequest request)
    {
        var job = new AgentJob
        {
            JobType = request.JobType,
            JobName = request.JobName,
            Description = request.Description,
            Parameters = request.Parameters,
            AgentId = request.TargetAgentId,
            Priority = request.Priority,
            Status = "Pending",
            CreatedByUserId = userId,
            CreatedByUserEmail = userEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.AgentJobs.Add(job);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Job {JobName} created by user {UserId}", job.JobName, userId);
        return job;
    }

    public async Task<List<AgentJob>> GetPendingJobsForAgentAsync(Guid agentId, int limit = 5)
    {
        return await _context.AgentJobs
            .Where(j => j.Status == "Pending" && (j.AgentId == null || j.AgentId == agentId))
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<AgentJob?> ClaimJobAsync(Guid agentId, Guid jobId)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null || agent.Status == "Offline") return null;
        
        if (agent.CurrentRunningJobs >= agent.MaxParallelJobs)
        {
            _logger.LogWarning("Agent {AgentId} at capacity ({Running}/{Max})", 
                agentId, agent.CurrentRunningJobs, agent.MaxParallelJobs);
            return null;
        }

        var rowsAffected = await _context.AgentJobs
            .Where(j => j.Id == jobId && j.Status == "Pending" && (j.AgentId == null || j.AgentId == agentId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.AgentId, agentId)
                .SetProperty(j => j.Status, "Running")
                .SetProperty(j => j.StartedAt, DateTime.UtcNow));

        if (rowsAffected == 0)
        {
            _logger.LogWarning("Job {JobId} could not be claimed - already taken or not pending", jobId);
            return null;
        }

        agent.CurrentRunningJobs++;
        if (agent.CurrentRunningJobs > 0)
            agent.Status = "Busy";

        await _context.SaveChangesAsync();

        var job = await _context.AgentJobs.FindAsync(jobId);
        _logger.LogInformation("Job {JobId} claimed by agent {AgentId}", jobId, agentId);
        return job;
    }

    public async Task<AgentJob?> UpdateJobProgressAsync(Guid agentId, Guid jobId, JobProgressRequest request)
    {
        var job = await _context.AgentJobs.FindAsync(jobId);
        if (job == null || job.Status != "Running") return null;
        
        if (job.AgentId != agentId)
        {
            _logger.LogWarning("Agent {AgentId} attempted to update job {JobId} owned by {OwnerId}", 
                agentId, jobId, job.AgentId);
            return null;
        }

        job.Progress = request.Progress;
        
        if (!string.IsNullOrEmpty(request.Log))
        {
            job.Logs = string.IsNullOrEmpty(job.Logs) 
                ? request.Log 
                : job.Logs + "\n" + request.Log;
        }

        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<AgentJob?> CompleteJobAsync(Guid agentId, Guid jobId, JobCompleteRequest request)
    {
        var job = await _context.AgentJobs.FindAsync(jobId);
        if (job == null || job.Status != "Running") return null;

        if (job.AgentId != agentId)
        {
            _logger.LogWarning("Agent {AgentId} attempted to complete job {JobId} owned by {OwnerId}", 
                agentId, jobId, job.AgentId);
            return null;
        }

        job.Status = request.Success ? "Completed" : "Failed";
        job.Progress = 100;
        job.Result = request.Result;
        job.ErrorMessage = request.ErrorMessage;
        job.CompletedAt = DateTime.UtcNow;

        var agent = await _context.Agents.FindAsync(agentId);
        if (agent != null)
        {
            agent.CurrentRunningJobs = Math.Max(0, agent.CurrentRunningJobs - 1);
            if (agent.CurrentRunningJobs == 0 && agent.Status == "Busy")
                agent.Status = "Online";
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Job {JobId} completed with status {Status}", jobId, job.Status);
        return job;
    }

    public async Task<List<AgentJob>> GetJobsAsync(string? userId = null, int limit = 100)
    {
        var query = _context.AgentJobs
            .Include(j => j.Agent)
            .AsQueryable();
            
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(j => j.CreatedByUserId == userId);
        }
        
        return await query
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<AgentJob?> GetJobByIdAsync(Guid jobId)
    {
        return await _context.AgentJobs
            .Include(j => j.Agent)
            .FirstOrDefaultAsync(j => j.Id == jobId);
    }

    public async Task<bool> CancelJobAsync(Guid jobId)
    {
        var job = await _context.AgentJobs.FindAsync(jobId);
        if (job == null || job.Status == "Completed" || job.Status == "Failed") return false;

        var wasRunning = job.Status == "Running";
        job.Status = "Cancelled";
        job.CompletedAt = DateTime.UtcNow;

        if (wasRunning && job.AgentId.HasValue)
        {
            var agent = await _context.Agents.FindAsync(job.AgentId.Value);
            if (agent != null)
            {
                agent.CurrentRunningJobs = Math.Max(0, agent.CurrentRunningJobs - 1);
                if (agent.CurrentRunningJobs == 0 && agent.Status == "Busy")
                    agent.Status = "Online";
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Job {JobId} cancelled", jobId);
        return true;
    }

    public async Task UpdateAgentStatusesAsync()
    {
        var cutoffTime = DateTime.UtcNow - HeartbeatTimeout;
        var staleAgents = await _context.Agents
            .Where(a => a.Status != "Offline" && a.LastHeartbeat < cutoffTime)
            .ToListAsync();

        foreach (var agent in staleAgents)
        {
            agent.Status = "Offline";
            agent.CurrentRunningJobs = 0;
        }

        if (staleAgents.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Marked {Count} agents as offline due to heartbeat timeout", staleAgents.Count);
        }
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
