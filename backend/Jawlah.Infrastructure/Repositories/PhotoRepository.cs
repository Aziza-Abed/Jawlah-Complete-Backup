using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Repositories;

public class PhotoRepository : Repository<Photo>, IPhotoRepository
{
    public PhotoRepository(JawlahDbContext context) : base(context)
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
        var photos = await GetPhotosByEntityAsync(entityType, entityId);
        _dbSet.RemoveRange(photos);
    }
}
