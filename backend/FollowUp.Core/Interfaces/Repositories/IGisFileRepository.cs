using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IGisFileRepository
{
    Task<GisFile?> GetByIdAsync(int id);
    Task<GisFile?> GetActiveByTypeAsync(GisFileType fileType);
    Task<IEnumerable<GisFile>> GetAllAsync();
    Task<IEnumerable<GisFile>> GetByTypeAsync(GisFileType fileType);
    Task<GisFile> AddAsync(GisFile gisFile);
    Task UpdateAsync(GisFile gisFile);
    Task DeleteAsync(int id);
    Task DeactivateByTypeAsync(GisFileType fileType);
}
