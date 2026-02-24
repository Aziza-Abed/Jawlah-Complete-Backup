using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IAppealRepository : IRepository<Appeal>
{
    // get all appeals for a specific user
    Task<IEnumerable<Appeal>> GetUserAppealsAsync(int userId);

    // get all pending appeals (for supervisors)
    Task<IEnumerable<Appeal>> GetPendingAppealsAsync();

    // get appeals by status
    Task<IEnumerable<Appeal>> GetAppealsByStatusAsync(AppealStatus status);

    // get appeals filtered by various criteria
    Task<IEnumerable<Appeal>> GetFilteredAppealsAsync(
        int? userId = null,
        AppealStatus? status = null,
        AppealType? appealType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    // check if an appeal already exists for an entity
    Task<bool> HasAppealForEntityAsync(string entityType, int entityId);

    // find appeal by evidence photo filename (for file access authorization)
    Task<Appeal?> GetByEvidencePhotoFilenameAsync(string filename);
}
