using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace FollowUp.Infrastructure.Repositories;

public class PhotoRepository : Repository<Photo>, IPhotoRepository
{
    public PhotoRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Photo>> GetPhotosByEntityAsync(string entityType, int entityId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.EntityType == entityType && p.EntityId == entityId)
            .OrderBy(p => p.OrderIndex)
            .ToListAsync();
    }

    public async Task DeletePhotosByEntityAsync(string entityType, int entityId)
    {
        var photos = await _dbSet
            .Where(p => p.EntityType == entityType && p.EntityId == entityId)
            .ToListAsync();
        _dbSet.RemoveRange(photos);
        await _context.SaveChangesAsync();
    }

    public async Task<Photo?> GetByFilenameAsync(string filename)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PhotoUrl != null && p.PhotoUrl.Contains(filename));
    }
}
