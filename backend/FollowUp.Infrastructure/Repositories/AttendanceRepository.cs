using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<Attendance?> GetTodayAttendanceAsync(int userId)
    {
        // define today's range
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // find the first attendance record for this user today
        // NOTE: No AsNoTracking because callers may update this entity
        return await _dbSet
            .Include(a => a.Zone)
            .Where(a => a.UserId == userId &&
                       a.CheckInEventTime >= today &&
                       a.CheckInEventTime < tomorrow)
            .OrderByDescending(a => a.CheckInEventTime)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Attendance>> GetUserAttendanceHistoryAsync(int userId, DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Zone)
            .Where(a => a.UserId == userId &&
                       a.CheckInEventTime >= fromDate &&
                       a.CheckInEventTime <= toDate)
            .OrderByDescending(a => a.CheckInEventTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetZoneAttendanceAsync(int zoneId, DateTime date)
    {
        var nextDay = date.AddDays(1);

        return await _dbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.ZoneId == zoneId &&
                       a.CheckInEventTime >= date &&
                       a.CheckInEventTime < nextDay)
            .OrderByDescending(a => a.CheckInEventTime)
            .ToListAsync();
    }

    public async Task<bool> HasActiveAttendanceAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _dbSet
            .AnyAsync(a => a.UserId == userId &&
                          a.Status == AttendanceStatus.CheckedIn &&
                          a.CheckInEventTime >= today &&
                          a.CheckInEventTime < tomorrow);
    }

    public async Task<Attendance?> GetActiveAttendanceAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _dbSet
            .Include(a => a.Zone)
            .Where(a => a.UserId == userId &&
                       a.Status == AttendanceStatus.CheckedIn &&
                       a.CheckInEventTime >= today &&
                       a.CheckInEventTime < tomorrow)
            .OrderByDescending(a => a.CheckInEventTime)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Attendance>> GetFilteredAttendanceAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Zone)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (zoneId.HasValue)
            query = query.Where(a => a.ZoneId == zoneId.Value);

        if (fromDate.HasValue)
            query = query.Where(a => a.CheckInEventTime >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CheckInEventTime <= toDate.Value);

        return await query
            .OrderByDescending(a => a.CheckInEventTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetPendingManualAttendanceAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Zone)
            .Where(a => a.IsManual && !a.IsValidated && a.ApprovedByUserId == null)
            .OrderByDescending(a => a.CheckInEventTime)
            .ToListAsync();
    }

    // Dashboard-optimized: Get today's attendance for specific workers
    // Uses direct WHERE clause instead of OPENJSON for better performance
    public async Task<IEnumerable<Attendance>> GetTodayAttendanceForWorkersAsync(IEnumerable<int> workerIds)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var workerIdSet = workerIds.ToHashSet();

        // Only load essential data for dashboard - Zone name only
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Zone)
            .Where(a => a.CheckInEventTime >= today &&
                       a.CheckInEventTime < tomorrow &&
                       workerIdSet.Contains(a.UserId))
            .Select(a => new Attendance
            {
                AttendanceId = a.AttendanceId,
                UserId = a.UserId,
                Status = a.Status,
                CheckInEventTime = a.CheckInEventTime,
                ZoneId = a.ZoneId,
                Zone = a.Zone == null ? null : new Zone { ZoneId = a.Zone.ZoneId, ZoneName = a.Zone.ZoneName }
            })
            .ToListAsync();
    }
}
