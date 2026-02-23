using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using Task = System.Threading.Tasks.Task;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;
using TaskPriority = FollowUp.Core.Enums.TaskPriority;

namespace FollowUp.Infrastructure.BackgroundServices;

public class TaskGenerationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskGenerationBackgroundService> _logger;

    public TaskGenerationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TaskGenerationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Generation Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Task Generation.");
            }

            try
            {
                await AutoCloseStaleAttendanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Attendance Auto-Close.");
            }

            // Check every 15 minutes
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task GenerateTasksAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();

        var now = DateTime.UtcNow; // Use UTC for consistency with all other DateTime fields in the database

        var templates = await dbContext.TaskTemplates
            .Where(t => t.IsActive)
            .ToListAsync(stoppingToken);

        // Get first admin user to use as default assignee (required FK)
        var defaultAssignee = await dbContext.Users
            .Where(u => u.MunicipalityId == templates.FirstOrDefault()!.MunicipalityId)
            .OrderBy(u => u.UserId)
            .FirstOrDefaultAsync(stoppingToken);

        if (defaultAssignee == null)
        {
            _logger.LogWarning("No users found for task generation — skipping");
            return;
        }

        foreach (var template in templates)
        {
            if (ShouldGenerateTask(template, now))
            {
                var task = new FollowUp.Core.Entities.Task
                {
                    Title = template.Title,
                    Description = template.Description,
                    MunicipalityId = template.MunicipalityId,
                    ZoneId = template.ZoneId,
                    AssignedToUserId = defaultAssignee.UserId, // Required FK — assign to first user, supervisor can reassign
                    Priority = TaskPriority.Medium,
                    Status = TaskStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    EventTime = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.Date.AddDays(1).Add(template.Time),
                };

                dbContext.Tasks.Add(task);
                template.LastGeneratedAt = now;

                _logger.LogInformation("Generated task from template {TemplateId} assigned to user {UserId}", template.Id, defaultAssignee.UserId);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private bool ShouldGenerateTask(TaskTemplate template, DateTime now)
    {
        // 1. Check Time
        // Allow a window, e.g., if now.TimeOfDay >= template.Time AND haven't run yet today.
        // Simple check: Is it past the scheduled time?
        if (now.TimeOfDay < template.Time)
            return false;

        // 2. Check Frequency
        if (template.LastGeneratedAt.HasValue)
        {
             var last = template.LastGeneratedAt.Value;
             
             if (template.Frequency == "Daily")
             {
                 // Should run if last run was not today
                 return last.Date < now.Date;
             }
             else if (template.Frequency == "Weekly")
             {
                 // Should run if last run was not this week (e.g., < now.AddDays(-6))?
                 // Simple weekly: Only run on the same DayOfWeek as CreatedAt (or config?)
                 // Let's assume consistent DayOfWeek logic.
                 // If last run was < 1 week ago, don't run.
                 // Actually, better: if last run was not today AND today is the correct day of week?
                 // Or just interval: (now - last).TotalDays >= 7?
                 
                 // Let's go with interval for robustness + check day of week matches CreatedAt
                 // If creation was on Monday, it runs every Monday.
                 if (now.DayOfWeek != template.CreatedAt.DayOfWeek)
                     return false;
                     
                 return last.Date < now.Date; // Ensure hasn't run today (which implies hasn't run this week due to day check)
             }
             else if (template.Frequency == "Monthly")
             {
                 if (now.Day != template.CreatedAt.Day)
                     return false;
                     
                 return last.Date < now.Date;
             }
        }
        else
        {
            // First run
            // Check if day matches for Weekly/Monthly
            if (template.Frequency == "Weekly" && now.DayOfWeek != template.CreatedAt.DayOfWeek) return false;
            if (template.Frequency == "Monthly" && now.Day != template.CreatedAt.Day) return false;
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Auto-close attendance records that have been open (CheckedIn) for more than 14 hours.
    /// This handles the case where a worker forgets to check out at end of day.
    /// Sets status to AutoClosed (3) so supervisors can distinguish from normal checkouts.
    /// Uses per-user ExpectedEndTime for the auto-close checkout time.
    /// </summary>
    private async Task AutoCloseStaleAttendanceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();

        var cutoff = DateTime.UtcNow.AddHours(-14);

        var staleRecords = await dbContext.Attendances
            .Include(a => a.User)
            .Where(a => a.Status == AttendanceStatus.CheckedIn
                     && a.CheckInEventTime < cutoff
                     && a.CheckOutEventTime == null)
            .ToListAsync(stoppingToken);

        if (!staleRecords.Any()) return;

        foreach (var record in staleRecords)
        {
            record.Status = AttendanceStatus.AutoClosed;

            // Use per-user ExpectedEndTime if available, fallback to 16:00
            var endTime = record.User?.ExpectedEndTime ?? new TimeSpan(16, 0, 0);
            record.CheckOutEventTime = record.CheckInEventTime.Date.Add(endTime);

            record.CheckOutSyncTime = DateTime.UtcNow;
            record.ValidationMessage = "تم إغلاق الحضور تلقائياً - لم يتم تسجيل الانصراف";
            record.AttendanceType = record.AttendanceType == "Late" ? "Late" : "AutoClosed";

            // Calculate work duration from check-in to auto-close time
            var duration = record.CheckOutEventTime.Value - record.CheckInEventTime;
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;
            if (duration > TimeSpan.FromHours(23))
                duration = TimeSpan.FromHours(23);
            record.WorkDuration = duration;
        }

        await dbContext.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Auto-closed {Count} stale attendance records", staleRecords.Count);
    }
}
