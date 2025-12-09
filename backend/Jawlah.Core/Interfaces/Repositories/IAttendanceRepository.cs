using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<Attendance?> GetTodayAttendanceAsync(int userId);
    Task<IEnumerable<Attendance>> GetUserAttendanceHistoryAsync(int userId, DateTime fromDate, DateTime toDate);
    Task<IEnumerable<Attendance>> GetZoneAttendanceAsync(int zoneId, DateTime date);
    Task<bool> HasActiveAttendanceAsync(int userId);
    Task<IEnumerable<Attendance>> GetFilteredAttendanceAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate);
}
