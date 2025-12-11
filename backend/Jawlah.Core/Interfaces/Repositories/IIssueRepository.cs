using Jawlah.Core.Entities;
using Jawlah.Core.Enums;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IIssueRepository : IRepository<Issue>
{
    Task<IEnumerable<Issue>> GetUserIssuesAsync(int userId);
    Task<IEnumerable<Issue>> GetIssuesByStatusAsync(IssueStatus status);
    Task<IEnumerable<Issue>> GetIssuesByTypeAsync(IssueType type);
    Task<IEnumerable<Issue>> GetCriticalIssuesAsync();
}
