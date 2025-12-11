using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Repositories;

public class IssueRepository : Repository<Issue>, IIssueRepository
{
    public IssueRepository(JawlahDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Issue>> GetUserIssuesAsync(int userId)
    {
        return await _dbSet
            .Include(i => i.Zone)
            .Include(i => i.ResolvedByUser)
            .Where(i => i.ReportedByUserId == userId)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Issue>> GetIssuesByStatusAsync(IssueStatus status)
    {
        return await _dbSet
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Issue>> GetIssuesByTypeAsync(IssueType type)
    {
        return await _dbSet
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Where(i => i.Type == type)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Issue>> GetCriticalIssuesAsync()
    {
        return await _dbSet
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Where(i => i.Severity == IssueSeverity.Critical &&
                       i.Status != IssueStatus.Resolved &&
                       i.Status != IssueStatus.Dismissed)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
    }
}
