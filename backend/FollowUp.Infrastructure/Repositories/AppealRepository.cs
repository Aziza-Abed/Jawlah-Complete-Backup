using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class AppealRepository : Repository<Appeal>, IAppealRepository
{
    public AppealRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Appeal>> GetUserAppealsAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.ReviewedByUser)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appeal>> GetPendingAppealsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.Status == AppealStatus.Pending)
            .OrderBy(a => a.SubmittedAt) // Oldest first for FIFO processing
            .ToListAsync();
    }

    public async Task<IEnumerable<Appeal>> GetAppealsByStatusAsync(AppealStatus status)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.ReviewedByUser)
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appeal>> GetFilteredAppealsAsync(
        int? userId = null,
        AppealStatus? status = null,
        AppealType? appealType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.ReviewedByUser)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (appealType.HasValue)
            query = query.Where(a => a.AppealType == appealType.Value);

        if (fromDate.HasValue)
            query = query.Where(a => a.SubmittedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.SubmittedAt <= toDate.Value);

        return await query
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<bool> HasAppealForEntityAsync(string entityType, int entityId)
    {
        return await _dbSet
            .AnyAsync(a => a.EntityType == entityType &&
                          a.EntityId == entityId &&
                          a.Status == AppealStatus.Pending);
    }
}
