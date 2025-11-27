using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudOps.Web.Models;

public class AgentPool
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsHosted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string CreatedByUserId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? CreatedByUserEmail { get; set; }
    
    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}

public class AgentLabel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid AgentId { get; set; }
    
    public Agent? Agent { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Value { get; set; }
}

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? PoolId { get; set; }
    
    public AgentPool? Pool { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Offline";
    
    [Required]
    [MaxLength(100)]
    public string ApiKey { get; set; } = string.Empty;
    
    public int MaxParallelJobs { get; set; } = 2;
    
    public int CurrentRunningJobs { get; set; } = 0;
    
    [MaxLength(100)]
    public string? HostName { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(100)]
    public string? OperatingSystem { get; set; }
    
    [MaxLength(50)]
    public string? AgentVersion { get; set; }
    
    [MaxLength(100)]
    public string? MachineName { get; set; }
    
    [MaxLength(50)]
    public string? Architecture { get; set; }
    
    [MaxLength(200)]
    public string? CpuModel { get; set; }
    
    public int? LogicalCores { get; set; }
    
    public double? MemoryGb { get; set; }
    
    public double? DiskSpaceGb { get; set; }
    
    public string? Capabilities { get; set; }
    
    public DateTime? LastCapabilitiesSync { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastHeartbeat { get; set; }
    
    public string CreatedByUserId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? CreatedByUserEmail { get; set; }
    
    public ICollection<AgentJob> Jobs { get; set; } = new List<AgentJob>();
    
    public ICollection<AgentLabel> Labels { get; set; } = new List<AgentLabel>();
}

public class AgentJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? AgentId { get; set; }
    
    public Agent? Agent { get; set; }
    
    public Guid? PoolId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string JobType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string JobName { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public string? Parameters { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    public int Progress { get; set; } = 0;
    
    public string? Result { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public string? Logs { get; set; }
    
    public int Priority { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public string CreatedByUserId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? CreatedByUserEmail { get; set; }
}

public class AgentPoolRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool IsHosted { get; set; } = false;
}

public class AgentRegistrationRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public Guid PoolId { get; set; }
    
    public int MaxParallelJobs { get; set; } = 2;
    
    public string? HostName { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? OperatingSystem { get; set; }
    
    public string? AgentVersion { get; set; }
    
    public string? MachineName { get; set; }
    
    public string? Architecture { get; set; }
    
    public string? CpuModel { get; set; }
    
    public int? LogicalCores { get; set; }
    
    public double? MemoryGb { get; set; }
    
    public double? DiskSpaceGb { get; set; }
    
    public List<AgentLabelRequest>? Labels { get; set; }
}

public class AgentLabelRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Value { get; set; }
}

public class AgentHeartbeatRequest
{
    public string Status { get; set; } = "Online";
    
    public int CurrentRunningJobs { get; set; } = 0;
    
    public string? HostName { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? OperatingSystem { get; set; }
    
    public string? AgentVersion { get; set; }
    
    public string? MachineName { get; set; }
    
    public string? Architecture { get; set; }
    
    public string? CpuModel { get; set; }
    
    public int? LogicalCores { get; set; }
    
    public double? MemoryGb { get; set; }
    
    public double? DiskSpaceGb { get; set; }
}

public class CreateJobRequest
{
    [Required]
    public string JobType { get; set; } = string.Empty;
    
    [Required]
    public string JobName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? Parameters { get; set; }
    
    public Guid? TargetAgentId { get; set; }
    
    public Guid? TargetPoolId { get; set; }
    
    public int Priority { get; set; } = 0;
}

public class JobProgressRequest
{
    public int Progress { get; set; }
    
    public string? Log { get; set; }
}

public class JobCompleteRequest
{
    public bool Success { get; set; }
    
    public string? Result { get; set; }
    
    public string? ErrorMessage { get; set; }
}

public class AgentPoolResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsHosted { get; set; }
    public int TotalAgents { get; set; }
    public int OnlineAgents { get; set; }
    public int OfflineAgents { get; set; }
    public int RunningJobs { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AgentResponse
{
    public Guid Id { get; set; }
    public Guid? PoolId { get; set; }
    public string? PoolName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int MaxParallelJobs { get; set; }
    public int CurrentRunningJobs { get; set; }
    public string? HostName { get; set; }
    public string? IpAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public string? AgentVersion { get; set; }
    public string? MachineName { get; set; }
    public string? Architecture { get; set; }
    public string? CpuModel { get; set; }
    public int? LogicalCores { get; set; }
    public double? MemoryGb { get; set; }
    public double? DiskSpaceGb { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public string? ApiKey { get; set; }
    public List<AgentLabelResponse> Labels { get; set; } = new();
}

public class AgentLabelResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class JobResponse
{
    public Guid Id { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentName { get; set; }
    public Guid? PoolId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Parameters { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
