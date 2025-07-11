using DevDash.DTO.UserTenant;
using DevDash.model;
namespace DevDash.Repository.IRepository
{
    public interface IUserTenantRepository : IJoinRepository<UserTenant>
    {
        Task JoinAsync(UserTenant entity, int userId);
        Task LeaveAsync(UserTenant entity,int userId);
        Task<string> InviteByEmailAsync(int inviterUserId, string email, int tenantId, string role);
        Task<string> AcceptInvitationAsync(string email, int tenantId);
        Task<string> UpdateUserRoleAsync(int inviterUserId, int tenantId, UpdateUserRoleDto dto);

    }
}
