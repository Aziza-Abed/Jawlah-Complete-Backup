using FollowUp.Core.Entities;

namespace FollowUp.Core.Interfaces.Repositories;

public interface ITeamRepository : IRepository<Team>
{
    // get team by ID with Department navigation loaded
    Task<Team?> GetByIdWithDepartmentAsync(int id);

    // get all teams with member count, optionally filtered by active and department
    Task<IEnumerable<(Team Team, int MemberCount)>> GetAllWithMemberCountAsync(bool? activeOnly = null, int? departmentId = null);

    // get a single team with member count
    Task<(Team? Team, int MemberCount)> GetByIdWithMemberCountAsync(int id);

    // check if a team code already exists (optionally exclude a given ID for updates)
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);

    // get count of members in a team
    Task<int> GetMemberCountAsync(int teamId);

    // get count of tasks assigned to a team
    Task<int> GetTaskCountAsync(int teamId);

    // check if a user is already team leader of another team
    Task<bool> IsLeaderOfAnotherTeamAsync(int userId, int? excludeTeamId = null);

    // get members of a team
    Task<IEnumerable<User>> GetTeamMembersAsync(int teamId);
}
