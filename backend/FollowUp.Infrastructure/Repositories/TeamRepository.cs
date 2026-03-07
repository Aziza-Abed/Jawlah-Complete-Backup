using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class TeamRepository : Repository<Team>, ITeamRepository
{
    public TeamRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<Team?> GetByIdWithDepartmentAsync(int id)
    {
        return await _dbSet
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.TeamId == id);
    }

    public async Task<IEnumerable<(Team Team, int MemberCount)>> GetAllWithMemberCountAsync(bool? activeOnly = null, int? departmentId = null)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(t => t.IsActive);

        if (departmentId.HasValue)
            query = query.Where(t => t.DepartmentId == departmentId.Value);

        var results = await query
            .Include(t => t.Department)
            .Include(t => t.TeamLeader)
            .OrderBy(t => t.Department.Name)
            .ThenBy(t => t.Name)
            .Select(t => new
            {
                Team = t,
                MemberCount = _context.Users.Count(u => u.TeamId == t.TeamId)
            })
            .ToListAsync();

        return results.Select(r => (r.Team, r.MemberCount));
    }

    public async Task<(Team? Team, int MemberCount)> GetByIdWithMemberCountAsync(int id)
    {
        var result = await _dbSet
            .Include(t => t.Department)
            .Include(t => t.TeamLeader)
            .Where(t => t.TeamId == id)
            .Select(t => new
            {
                Team = t,
                MemberCount = _context.Users.Count(u => u.TeamId == t.TeamId)
            })
            .OrderBy(x => x.Team.TeamId)
            .FirstOrDefaultAsync();

        return result == null ? (null, 0) : (result.Team, result.MemberCount);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _dbSet.Where(t => t.Code == code);
        if (excludeId.HasValue)
            query = query.Where(t => t.TeamId != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<int> GetMemberCountAsync(int teamId)
    {
        return await _context.Users.CountAsync(u => u.TeamId == teamId);
    }

    public async Task<int> GetTaskCountAsync(int teamId)
    {
        return await _context.Tasks.CountAsync(t => t.TeamId == teamId);
    }

    public async Task<bool> IsLeaderOfAnotherTeamAsync(int userId, int? excludeTeamId = null)
    {
        var query = _dbSet.Where(t => t.TeamLeaderId == userId);
        if (excludeTeamId.HasValue)
            query = query.Where(t => t.TeamId != excludeTeamId.Value);
        return await query.AnyAsync();
    }

    public async Task<IEnumerable<User>> GetTeamMembersAsync(int teamId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.TeamId == teamId)
            .ToListAsync();
    }
}
