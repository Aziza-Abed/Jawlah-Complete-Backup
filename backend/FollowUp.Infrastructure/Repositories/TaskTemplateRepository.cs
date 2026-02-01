using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class TaskTemplateRepository : ITaskTemplateRepository
{
    private readonly FollowUpDbContext _context;

    public TaskTemplateRepository(FollowUpDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskTemplate>> GetAllAsync(int municipalityId)
    {
        return await _context.TaskTemplates
            .Include(t => t.Zone)
            .Where(t => t.MunicipalityId == municipalityId)
            .OrderByDescending(t => t.Id)
            .ToListAsync();
    }

    public async Task<TaskTemplate?> GetByIdAsync(int id)
    {
        return await _context.TaskTemplates
            .Include(t => t.Zone)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TaskTemplate> AddAsync(TaskTemplate template)
    {
        await _context.TaskTemplates.AddAsync(template);
        await _context.SaveChangesAsync();
        // Load navigation properties to return complete object
        await _context.Entry(template).Reference(t => t.Zone).LoadAsync();
        return template;
    }

    public async Task UpdateAsync(TaskTemplate template)
    {
        _context.TaskTemplates.Update(template);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var template = await GetByIdAsync(id);
        if (template != null)
        {
            _context.TaskTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }
}
