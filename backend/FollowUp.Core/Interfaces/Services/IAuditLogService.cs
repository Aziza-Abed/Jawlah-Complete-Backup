using FollowUp.Core.Entities;

namespace FollowUp.Core.Interfaces.Services;

public interface IAuditLogService
{
    System.Threading.Tasks.Task LogAsync(int? userId, string? username, string action, string? details = null, string? ipAddress = null, string? userAgent = null);
    System.Threading.Tasks.Task<List<AuditLog>> GetRecentLogsAsync(int count = 100, int? userId = null, string? action = null);
    System.Threading.Tasks.Task<List<AuditLog>> GetLogsByDateRangeAsync(DateTime from, DateTime to);
}
