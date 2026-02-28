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

        if (templates.Count == 0)
        {
            _logger.LogDebug("No active task templates found — skipping");
            return;
        }

        // Get first Worker in this municipality as default assignee (supervisor can reassign)
        var defaultAssignee = await dbContext.Users
            .Where(u => u.MunicipalityId == templates[0].MunicipalityId && u.Role == UserRole.Worker)
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
        // skip if scheduled time hasn't passed yet today
        if (now.TimeOfDay < template.Time)
            return false;

        // for Weekly: only run on the same day of week as when template was created
        if (template.Frequency == "Weekly" && now.DayOfWeek != template.CreatedAt.DayOfWeek)
            return false;

        // for Monthly: only run on the same day of month as when template was created
        if (template.Frequency == "Monthly" && now.Day != template.CreatedAt.Day)
            return false;

        // first run — day checks passed above, generate
        if (!template.LastGeneratedAt.HasValue)
            return true;

        // already ran today — skip
        return template.LastGeneratedAt.Value.Date < now.Date;
    }

    // auto-close attendance records that have been open (CheckedIn) for more than 14 hours
    // handles the case where a worker forgets to check out at end of day
    // sets status to AutoClosed (3) so supervisors can distinguish from normal checkouts
    // uses per-user ExpectedEndTime for the auto-close checkout time
    // uses raw SQL to avoid EF Core SELECT * query generation issues
    private async Task AutoCloseStaleAttendanceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();

        var cutoff = DateTime.UtcNow.AddHours(-14);
        var now = DateTime.UtcNow;

        // Raw SQL with CTE: computes per-user checkout time, then updates in one atomic operation.
        // This avoids EF Core's SELECT * which triggers a column mapping issue with AccuracyMeters.
        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            ;WITH Stale AS (
                SELECT a.AttendanceId, a.CheckInEventTime, a.AttendanceType,
                       DATEADD(
                           SECOND,
                           DATEDIFF(SECOND, CAST('00:00:00' AS TIME), COALESCE(u.ExpectedEndTime, CAST('16:00:00' AS TIME))),
                           CAST(CAST(a.CheckInEventTime AS DATE) AS DATETIME2)
                       ) AS ComputedCheckout
                FROM Attendances a
                LEFT JOIN Users u ON a.UserId = u.UserId
                WHERE a.Status = 1
                  AND a.CheckInEventTime < {cutoff}
                  AND a.CheckOutEventTime IS NULL
            )
            UPDATE a SET
                a.Status = 3,
                a.CheckOutEventTime = s.ComputedCheckout,
                a.CheckOutSyncTime = {now},
                a.ValidationMessage = N'تم إغلاق الحضور تلقائياً - لم يتم تسجيل الانصراف',
                a.AttendanceType = CASE WHEN s.AttendanceType = N'Late' THEN N'Late' ELSE N'AutoClosed' END,
                a.WorkDuration = CASE
                    WHEN s.ComputedCheckout <= a.CheckInEventTime THEN CAST('00:00:00' AS TIME)
                    WHEN DATEDIFF(HOUR, a.CheckInEventTime, s.ComputedCheckout) >= 23 THEN CAST('23:00:00' AS TIME)
                    ELSE CAST(DATEADD(SECOND, DATEDIFF(SECOND, a.CheckInEventTime, s.ComputedCheckout), CAST('00:00:00' AS DATETIME)) AS TIME)
                END
            FROM Attendances a
            INNER JOIN Stale s ON a.AttendanceId = s.AttendanceId", stoppingToken);

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Auto-closed {Count} stale attendance records", rowsAffected);
        }
    }
}
