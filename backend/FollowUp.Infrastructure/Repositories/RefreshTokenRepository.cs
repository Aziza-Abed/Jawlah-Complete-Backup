using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace FollowUp.Infrastructure.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<IEnumerable<RefreshToken>> GetUserTokensAsync(int userId)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
    }

    public async Task RevokeAllUserTokensAsync(int userId)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
