using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly JawlahDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(JawlahDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async System.Threading.Tasks.Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async System.Threading.Tasks.Task<IEnumerable<T>> GetAllAsync()
    {
        // just get everything from the table
        return await _dbSet.ToListAsync();
    }

    public virtual async System.Threading.Tasks.Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async System.Threading.Tasks.Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        return entities;
    }

    public virtual System.Threading.Tasks.Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public virtual System.Threading.Tasks.Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public virtual async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
