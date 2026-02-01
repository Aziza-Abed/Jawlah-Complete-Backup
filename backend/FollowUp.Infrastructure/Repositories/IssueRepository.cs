using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class IssueRepository : Repository<Issue>, IIssueRepository
{
    public IssueRepository(FollowUpDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<Issue>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Include(i => i.ResolvedByUser)
            .Include(i => i.Photos)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public override async Task<Issue?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Include(i => i.ResolvedByUser)
            .Include(i => i.Photos)
            .FirstOrDefaultAsync(i => i.IssueId == id);
    }

    public async Task<IEnumerable<Issue>> GetUserIssuesAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Include(i => i.ResolvedByUser)
            .Include(i => i.Photos)
            .Where(i => i.ReportedByUserId == userId)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Issue>> GetIssuesByStatusAsync(IssueStatus status)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Include(i => i.Photos)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Issue>> GetIssuesByTypeAsync(IssueType type)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Include(i => i.Photos)
            .Where(i => i.Type == type)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Issue>> GetCriticalIssuesAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Include(i => i.Photos)
            .Where(i => i.Severity == IssueSeverity.Critical &&
                       i.Status != IssueStatus.Resolved &&
                       i.Status != IssueStatus.Dismissed)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Issue>> GetIssuesModifiedAfterAsync(int userId, DateTime lastSyncTime)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(i => i.ReportedByUser)
            .Include(i => i.Zone)
            .Include(i => i.Photos)
            .Where(i => i.ReportedByUserId == userId && i.SyncTime > lastSyncTime)
            .ToListAsync();
    }

    // Dashboard optimized: Get stats using database-level COUNT instead of loading entities
    public async Task<IssueStatsDto> GetIssueStatsAsync(IEnumerable<int> workerIds, DateTime today)
    {
        var workerIdSet = workerIds.ToList();
        var tomorrow = today.AddDays(1);

        // Single query with conditional aggregation - much faster than loading all entities
        var stats = await _dbSet
            .AsNoTracking()
            .Where(i => workerIdSet.Contains(i.ReportedByUserId))
            .GroupBy(i => 1) // Group all into one
            .Select(g => new IssueStatsDto
            {
                ReportedToday = g.Count(i => i.ReportedAt >= today && i.ReportedAt < tomorrow),
                Unresolved = g.Count(i => i.Status != IssueStatus.Resolved && i.Status != IssueStatus.Dismissed)
            })
            .FirstOrDefaultAsync();

        return stats ?? new IssueStatsDto();
    }
}
