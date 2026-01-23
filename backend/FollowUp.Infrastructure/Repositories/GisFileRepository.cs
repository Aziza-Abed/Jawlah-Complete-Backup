using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Infrastructure.Repositories;

public class GisFileRepository : Repository<GisFile>, IGisFileRepository
{
    public GisFileRepository(FollowUpDbContext context) : base(context)
    {
    }

    public new async Task<GisFile?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(g => g.UploadedBy)
            .FirstOrDefaultAsync(g => g.GisFileId == id);
    }

    public async Task<GisFile?> GetActiveByTypeAsync(GisFileType fileType)
    {
        return await _dbSet
            .Include(g => g.UploadedBy)
            .FirstOrDefaultAsync(g => g.FileType == fileType && g.IsActive);
    }

    public new async Task<IEnumerable<GisFile>> GetAllAsync()
    {
        return await _dbSet
            .Include(g => g.UploadedBy)
            .OrderByDescending(g => g.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<GisFile>> GetByTypeAsync(GisFileType fileType)
    {
        return await _dbSet
            .Include(g => g.UploadedBy)
            .Where(g => g.FileType == fileType)
            .OrderByDescending(g => g.UploadedAt)
            .ToListAsync();
    }

    public new async Task<GisFile> AddAsync(GisFile gisFile)
    {
        await _dbSet.AddAsync(gisFile);
        await _context.SaveChangesAsync();
        return gisFile;
    }

    public new async Task UpdateAsync(GisFile gisFile)
    {
        _dbSet.Update(gisFile);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeactivateByTypeAsync(GisFileType fileType)
    {
        var activeFiles = await _dbSet
            .Where(g => g.FileType == fileType && g.IsActive)
            .ToListAsync();

        foreach (var file in activeFiles)
        {
            file.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }
}
