using FollowUp.Core.Entities;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IDepartmentRepository : IRepository<Department>
{
    // get all departments for a specific municipality
    Task<IEnumerable<Department>> GetByMunicipalityAsync(int municipalityId);

    // get all departments ordered by name, optionally filtered to active only
    Task<IEnumerable<(Department Department, int UserCount)>> GetAllWithUserCountAsync(bool? activeOnly = null);

    // get a single department with its user count
    Task<(Department? Department, int UserCount)> GetByIdWithUserCountAsync(int id);

    // check if a department code already exists (optionally exclude a given ID for updates)
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);

    // get count of users assigned to a department
    Task<int> GetUserCountAsync(int departmentId);
}
