using Jawlah.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IPhotoRepository : IRepository<Photo>
{
    Task<IEnumerable<Photo>> GetPhotosByEntityAsync(string entityType, int entityId);
    Task DeletePhotosByEntityAsync(string entityType, int entityId);
    Task<Photo?> GetByFilenameAsync(string filename);
}
