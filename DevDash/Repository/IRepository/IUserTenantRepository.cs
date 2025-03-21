using DevDash.model;
namespace DevDash.Repository.IRepository
{
    public interface IUserTenantRepository : IJoinRepository<UserTenant>
    {
        Task JoinAsync(UserTenant entity, string userId);

        Task LeaveAsync(UserTenant entity,string userId);

    }
}
