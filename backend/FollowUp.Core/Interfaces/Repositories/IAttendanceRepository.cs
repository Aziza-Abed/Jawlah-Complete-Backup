using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<Attendance?> GetTodayAttendanceAsync(int userId);
    Task<IEnumerable<Attendance>> GetUserAttendanceHistoryAsync(int userId, DateTime fromDate, DateTime toDate);
    Task<IEnumerable<Attendance>> GetZoneAttendanceAsync(int zoneId, DateTime date);
    Task<bool> HasActiveAttendanceAsync(int userId);
    Task<Attendance?> GetActiveAttendanceAsync(int userId);
    Task<IEnumerable<Attendance>> GetFilteredAttendanceAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<Attendance>> GetPendingManualAttendanceAsync();

    // Dashboard-optimized: Get today's attendance for specific workers with minimal includes
    Task<IEnumerable<Attendance>> GetTodayAttendanceForWorkersAsync(IEnumerable<int> workerIds);
}
