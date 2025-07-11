using DevDash.model;
namespace DevDash.Repository.IRepository
{
    public interface IUserTenantRepository : IJoinRepository<UserTenant>,IRepository<UserTenant>
    {
        Task JoinAsync(UserTenant entity, int userId);

        Task LeaveAsync(UserTenant entity,int userId);

    }
}
