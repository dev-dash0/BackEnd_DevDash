using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface ITenantRepository : IRepository<Tenant>
    {
        Task<Tenant> UpdateAsync(Tenant tenant);
        Task RemoveAsync(Tenant tenant);
    }
}
