using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IAppealRepository : IRepository<Appeal>
{
    /// <summary>
    /// Get all appeals for a specific user
    /// </summary>
    Task<IEnumerable<Appeal>> GetUserAppealsAsync(int userId);

    /// <summary>
    /// Get all pending appeals (for supervisors)
    /// </summary>
    Task<IEnumerable<Appeal>> GetPendingAppealsAsync();

    /// <summary>
    /// Get appeals by status
    /// </summary>
    Task<IEnumerable<Appeal>> GetAppealsByStatusAsync(AppealStatus status);

    /// <summary>
    /// Get appeals filtered by various criteria
    /// </summary>
    Task<IEnumerable<Appeal>> GetFilteredAppealsAsync(
        int? userId = null,
        AppealStatus? status = null,
        AppealType? appealType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Check if an appeal already exists for an entity
    /// </summary>
    Task<bool> HasAppealForEntityAsync(string entityType, int entityId);
}
