using Microsoft.EntityFrameworkCore;
using CloudOps.Shared.Models;
using System.Text.Json;

namespace CloudOps.Shared.Data;

public class CloudOpsDbContext : DbContext
{
    public CloudOpsDbContext(DbContextOptions<CloudOpsDbContext> options) : base(options) { }

    public DbSet<TaskEntity> Tasks { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<Models.Environment> Environments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.Property(e => e.Steps).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<TaskStep>>(v, (JsonSerializerOptions?)null) ?? new List<TaskStep>()
            );
            entity.Property(e => e.Metadata).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
            );
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmailRecipients).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
            entity.HasMany(e => e.Environments).WithOne().HasForeignKey(e => e.ProjectId);
        });

        modelBuilder.Entity<Models.Environment>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed data removed - will be added via API endpoints
    }
}
