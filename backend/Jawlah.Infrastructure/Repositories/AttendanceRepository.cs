using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Repositories;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(JawlahDbContext context) : base(context)
    {
    }

    public async Task<Attendance?> GetTodayAttendanceAsync(int userId)
    {
        //
        var today = DateTime.UtcNow.Date;  // Midnight UTC today
        var tomorrow = today.AddDays(1);   // Midnight UTC tomorrow

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
            .Include(a => a.User)
            .Where(a => a.ZoneId == zoneId &&
                       a.CheckInEventTime >= date &&
                       a.CheckInEventTime < nextDay)
            .OrderByDescending(a => a.CheckInEventTime)
            .ToListAsync();
    }

    public async Task<bool> HasActiveAttendanceAsync(int userId)
    {
        //
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _dbSet
            .AnyAsync(a => a.UserId == userId &&
                          a.Status == AttendanceStatus.CheckedIn &&
                          a.CheckInEventTime >= today &&
                          a.CheckInEventTime < tomorrow);
    }

    public async Task<IEnumerable<Attendance>> GetFilteredAttendanceAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _dbSet
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
}
