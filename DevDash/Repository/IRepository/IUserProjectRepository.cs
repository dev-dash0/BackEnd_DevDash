using DevDash.DTO.UserTenant;
using DevDash.model;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository.IRepository
{
    public interface IUserProjectRepository : IJoinRepository<UserProject>, IRepository<UserProject>
    {
        Task<string> InviteByEmailAsync(int inviterUserId, string email, int projectId, string role);
        Task<string> AcceptInvitationAsync(string email, int projectId);
        Task<string> UpdateUserRoleAsync(int inviterUserId, int projectId, UpdateUserRoleDto dto);
        Task JoinAsync(UserProject entity, int userId);

        Task LeaveAsync(UserProject entity, int userId);

    }
}
