using CloudOps.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudOps.Web.Data;

public class ActivityDbContext : DbContext
{
    public ActivityDbContext(DbContextOptions<ActivityDbContext> options) : base(options) { }

    public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
    public DbSet<Agent> Agents { get; set; } = null!;
    public DbSet<AgentJob> AgentJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.StartTime);
            entity.Property(e => e.TaskName).HasMaxLength(200);
            entity.Property(e => e.TaskType).HasMaxLength(100);
            entity.Property(e => e.SubscriptionName).HasMaxLength(200);
            entity.Property(e => e.SubscriptionId).HasMaxLength(100);
            entity.Property(e => e.ResourceName).HasMaxLength(300);
            entity.Property(e => e.SubResourceName).HasMaxLength(300);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserEmail).HasMaxLength(200);
            entity.Property(e => e.UserId).HasMaxLength(100);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ApiKey).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ApiKey).HasMaxLength(100);
            entity.Property(e => e.HostName).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.AgentVersion).HasMaxLength(50);
            entity.Property(e => e.CreatedByUserEmail).HasMaxLength(200);
        });

        modelBuilder.Entity<AgentJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.JobType).HasMaxLength(100);
            entity.Property(e => e.JobName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedByUserEmail).HasMaxLength(200);
            entity.HasOne(e => e.Agent)
                  .WithMany(a => a.Jobs)
                  .HasForeignKey(e => e.AgentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
