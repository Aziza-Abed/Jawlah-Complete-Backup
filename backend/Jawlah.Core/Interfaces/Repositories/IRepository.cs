namespace Jawlah.Core.Interfaces.Repositories;

public interface IRepository<T> where T : class
{
    System.Threading.Tasks.Task<T?> GetByIdAsync(int id);
    System.Threading.Tasks.Task<IEnumerable<T>> GetAllAsync();
    System.Threading.Tasks.Task<T> AddAsync(T entity);
    System.Threading.Tasks.Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    System.Threading.Tasks.Task UpdateAsync(T entity);
    System.Threading.Tasks.Task DeleteAsync(T entity);
    System.Threading.Tasks.Task SaveChangesAsync();
}
