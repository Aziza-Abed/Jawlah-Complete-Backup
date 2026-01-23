using FollowUp.Core.Entities;
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

            // Check every 15 minutes
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task GenerateTasksAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();

        var now = DateTime.Now; // Use local time or Utc based on requirements. Assuming Local for specific times like 08:00 AM.
        // If Time property in TaskTemplate is intended to be local time of the municipality

        var templates = await dbContext.TaskTemplates
            .Where(t => t.IsActive)
            .ToListAsync(stoppingToken);

        foreach (var template in templates)
        {
            if (ShouldGenerateTask(template, now))
            {
                // Create Task
                var task = new FollowUp.Core.Entities.Task
                {
                    Title = template.Title,
                    Description = template.Description,
                    MunicipalityId = template.MunicipalityId,
                    ZoneId = template.ZoneId,
                    Priority = TaskPriority.Medium, // Default
                    Status = TaskStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    DueDate = DateTime.Now.Date.AddDays(1).Add(template.Time), // Due next day? or same day? Let's say due end of day or specific logic.
                    // If generated at 8am, maybe due by 4pm?
                };
                
                // Assign to supervisor or keep unassigned?
                // Logic: assigning to "system" or specific user?
                // For V1: leave unassigned, let admin/supervisor assign. Or auto-assign via other logic.
                
                dbContext.Tasks.Add(task);
                template.LastGeneratedAt = now;
                
                _logger.LogInformation("Generated task from template {TemplateId}", template.Id);
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
}
