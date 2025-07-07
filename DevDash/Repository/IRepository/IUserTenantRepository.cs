using DevDash.model;
namespace DevDash.Repository.IRepository
{
    public interface IUserTenantRepository : IJoinRepository<UserTenant>
    {
        Task JoinAsync(UserTenant entity, int userId);

        Task LeaveAsync(UserTenant entity,int userId);

        Task InviteByEmailAsync(string email, int tenantId, string role);

        Task AcceptInvitationAsync(string email, int userId);

    }
}
