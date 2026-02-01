using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Repositories;

public interface ITaskTemplateRepository
{
    Task<IEnumerable<TaskTemplate>> GetAllAsync(int municipalityId);
    Task<TaskTemplate?> GetByIdAsync(int id);
    Task<TaskTemplate> AddAsync(TaskTemplate template);
    Task UpdateAsync(TaskTemplate template);
    Task DeleteAsync(int id);
}
