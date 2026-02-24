using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IMunicipalityRepository : IRepository<Municipality>
{
    // gets a municipality by its unique code
    Task<Municipality?> GetByCodeAsync(string code);

    // gets all active municipalities
    Task<IEnumerable<Municipality>> GetActiveAsync();

    // checks if a code is already in use
    Task<bool> CodeExistsAsync(string code);
}
