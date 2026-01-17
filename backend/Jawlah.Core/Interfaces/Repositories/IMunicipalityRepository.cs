using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IMunicipalityRepository : IRepository<Municipality>
{
    /// <summary>
    /// Gets a municipality by its unique code
    /// </summary>
    Task<Municipality?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets all active municipalities
    /// </summary>
    Task<IEnumerable<Municipality>> GetActiveAsync();

    /// <summary>
    /// Checks if a code is already in use
    /// </summary>
    Task<bool> CodeExistsAsync(string code);
}
