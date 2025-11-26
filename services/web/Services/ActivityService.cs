using CloudOps.Web.Data;
using CloudOps.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudOps.Web.Services;

public interface IActivityService
{
    Task<ActivityLog> LogActivityAsync(ActivityLog activity);
    Task<ActivityLog?> UpdateActivityAsync(Guid id, string status, int? itemsProcessed = null, string? errorMessage = null);
    Task<List<ActivityLog>> GetUserActivitiesAsync(string userId, int limit = 50);
    Task<ActivityLog?> GetActivityByIdAsync(Guid id);
}

public class ActivityService : IActivityService
{
    private readonly ActivityDbContext _context;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(ActivityDbContext context, ILogger<ActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ActivityLog> LogActivityAsync(ActivityLog activity)
    {
        _context.ActivityLogs.Add(activity);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Activity logged: {TaskName} for user {UserEmail}", activity.TaskName, activity.UserEmail);
        return activity;
    }

    public async Task<ActivityLog?> UpdateActivityAsync(Guid id, string status, int? itemsProcessed = null, string? errorMessage = null)
    {
        var activity = await _context.ActivityLogs.FindAsync(id);
        if (activity == null) return null;

        activity.Status = status;
        activity.EndTime = DateTime.UtcNow;
        if (itemsProcessed.HasValue) activity.ItemsProcessed = itemsProcessed;
        if (!string.IsNullOrEmpty(errorMessage)) activity.ErrorMessage = errorMessage;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Activity updated: {Id} -> {Status}", id, status);
        return activity;
    }

    public async Task<List<ActivityLog>> GetUserActivitiesAsync(string userId, int limit = 50)
    {
        return await _context.ActivityLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ActivityLog?> GetActivityByIdAsync(Guid id)
    {
        return await _context.ActivityLogs.FindAsync(id);
    }
}
