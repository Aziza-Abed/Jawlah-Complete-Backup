using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Infrastructure.Repositories;

// repository for refresh token operations
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly FollowUpDbContext _context;
    private readonly DbSet<RefreshToken> _dbSet;

    public RefreshTokenRepository(FollowUpDbContext context)
    {
        _context = context;
        _dbSet = context.Set<RefreshToken>();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<RefreshToken?> GetActiveTokenByUserIdAsync(int userId, string? deviceId = null)
    {
        var query = _dbSet.Where(rt =>
            rt.UserId == userId &&
            rt.RevokedAt == null &&
            rt.ExpiresAt > DateTime.UtcNow);

        if (!string.IsNullOrEmpty(deviceId))
        {
            query = query.Where(rt => rt.DeviceId == deviceId);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        await _dbSet.AddAsync(refreshToken);
    }

    public Task UpdateAsync(RefreshToken refreshToken)
    {
        _dbSet.Update(refreshToken);
        return Task.CompletedTask;
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
    }

    public async Task RevokeTokenAsync(string token)
    {
        var refreshToken = await _dbSet.FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
        }
    }

    public async Task DeleteExpiredTokensAsync()
    {
        var expiredTokens = await _dbSet
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow.AddDays(-7)) // Keep 7 days for audit
            .ToListAsync();

        _dbSet.RemoveRange(expiredTokens);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
