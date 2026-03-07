using FollowUp.Core.Constants;
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

            // Check every N minutes (configured in AppConstants)
            await Task.Delay(TimeSpan.FromMinutes(AppConstants.BackgroundServiceCheckIntervalMinutes), stoppingToken);
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

        foreach (var template in templates)
        {
            if (!ShouldGenerateTask(template, now))
                continue;

            // Determine assignment from the template itself
            int assignedToUserId;
            int? teamId = null;

            if (template.IsTeamTask && template.DefaultTeamId.HasValue)
            {
                // Team task: pick the first active member of the team to satisfy the NOT NULL FK.
                // The real "owner" is expressed via TeamId; the FK user is just a required placeholder.
                var teamMember = await dbContext.Users
                    .Where(u => u.TeamId == template.DefaultTeamId.Value
                             && u.Status == FollowUp.Core.Enums.UserStatus.Active)
                    .OrderBy(u => u.UserId)
                    .FirstOrDefaultAsync(stoppingToken);

                if (teamMember == null)
                {
                    _logger.LogWarning(
                        "Template {TemplateId} is a team task but team {TeamId} has no active members — skipping",
                        template.Id, template.DefaultTeamId.Value);
                    continue;
                }

                assignedToUserId = teamMember.UserId;
                teamId = template.DefaultTeamId.Value;
            }
            else if (!template.IsTeamTask && template.DefaultAssignedToUserId.HasValue)
            {
                // Individual task: use the designated worker
                assignedToUserId = template.DefaultAssignedToUserId.Value;
            }
            else
            {
                // No default assignee configured — skip rather than produce an invalid FK value.
                // Supervisor must set DefaultAssignedToUserId or DefaultTeamId on the template.
                _logger.LogWarning(
                    "Template {TemplateId} has no default assignee — skipping. Update the template to add a worker or team.",
                    template.Id);
                continue;
            }

            var task = new FollowUp.Core.Entities.Task
            {
                Title = template.Title,
                Description = template.Description,
                MunicipalityId = template.MunicipalityId,
                ZoneId = template.ZoneId,
                AssignedToUserId = assignedToUserId,
                AssignedByUserId = null, // system-generated
                TeamId = teamId,
                IsTeamTask = template.IsTeamTask,
                Priority = template.Priority,
                Status = TaskStatus.Pending,
                TaskType = template.TaskType,
                RequiresPhotoProof = template.RequiresPhotoProof,
                EstimatedDurationMinutes = template.EstimatedDurationMinutes,
                LocationDescription = template.LocationDescription,
                CreatedAt = DateTime.UtcNow,
                EventTime = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.Date.AddDays(1).Add(template.Time),
                SyncTime = DateTime.UtcNow,
                IsSynced = true,
                SyncVersion = 1,
            };

            dbContext.Tasks.Add(task);
            template.LastGeneratedAt = now;

            // Save per template to prevent duplicates if a later save fails
            try
            {
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation(
                    "Generated task from template {TemplateId} — {AssignType} {AssignId}",
                    template.Id,
                    template.IsTeamTask ? "team" : "user",
                    template.IsTeamTask ? (object?)teamId : assignedToUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save generated task for template {TemplateId}", template.Id);
            }
        }
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

    // auto-close attendance records that have been open (CheckedIn) for more than AttendanceAutoCloseHours
    // handles the case where a worker forgets to check out at end of day
    // sets status to AutoClosed (3) so supervisors can distinguish from normal checkouts
    // uses per-user ExpectedEndTime for the auto-close checkout time
    // uses raw SQL to avoid EF Core SELECT * query generation issues
    private async Task AutoCloseStaleAttendanceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();

        var cutoff = DateTime.UtcNow.AddHours(-AppConstants.AttendanceAutoCloseHours);
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
                    WHEN DATEDIFF(HOUR, a.CheckInEventTime, s.ComputedCheckout) >= {AppConstants.MaxWorkDurationHours} THEN CAST(CONCAT({AppConstants.MaxWorkDurationHours}, ':00:00') AS TIME)
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
