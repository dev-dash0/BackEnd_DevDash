using DevDash.model;
namespace DevDash.Repository.IRepository
{
    public interface IUserTenantRepository : IJoinRepository<UserTenant>
    {
        public Task LeaveAsync(UserTenant entity);

    }
}
