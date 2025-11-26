using CloudOps.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudOps.Web.Data;

public class ActivityDbContext : DbContext
{
    public ActivityDbContext(DbContextOptions<ActivityDbContext> options) : base(options) { }

    public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;

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
    }
}
