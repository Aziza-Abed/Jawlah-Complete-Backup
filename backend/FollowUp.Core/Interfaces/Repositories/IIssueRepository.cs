using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IIssueRepository : IRepository<Issue>
{
    Task<IEnumerable<Issue>> GetUserIssuesAsync(int userId);
    Task<IEnumerable<Issue>> GetIssuesByStatusAsync(IssueStatus status);
    Task<IEnumerable<Issue>> GetIssuesByTypeAsync(IssueType type);
    Task<IEnumerable<Issue>> GetCriticalIssuesAsync();
    Task<IEnumerable<Issue>> GetIssuesModifiedAfterAsync(int userId, DateTime lastSyncTime);

    // Dashboard optimized methods - use database-level aggregation
    Task<IssueStatsDto> GetIssueStatsAsync(IEnumerable<int> workerIds, DateTime today);
}

// DTO for dashboard issue stats (avoids loading all entities)
public class IssueStatsDto
{
    public int ReportedToday { get; set; }
    public int Unresolved { get; set; }
}
