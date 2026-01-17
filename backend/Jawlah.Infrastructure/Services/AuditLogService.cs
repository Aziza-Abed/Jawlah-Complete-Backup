using Jawlah.Core.Entities;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Services;

// UR23: Simple audit logging service
public class AuditLogService
{
    private readonly JawlahDbContext _context;

    public AuditLogService(JawlahDbContext context)
    {
        _context = context;
    }

    // Log an action
    public async Task LogAsync(int? userId, string? username, string action, string? details = null, string? ipAddress = null, string? userAgent = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    // Get recent logs (for admin dashboard)
    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100, int? userId = null, string? action = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .Include(l => l.User)
            .ToListAsync();
    }

    // Get logs by date range
    public async Task<List<AuditLog>> GetLogsByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _context.AuditLogs
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .OrderByDescending(l => l.CreatedAt)
            .Include(l => l.User)
            .ToListAsync();
    }
}
