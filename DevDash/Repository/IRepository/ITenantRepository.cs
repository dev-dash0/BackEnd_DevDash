using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface ITenantRepository : IRepository<Tenant>
    {
        Task<Tenant> CreateAsync(Tenant tenant, int ownerId);
        Task<Tenant> UpdateAsync(Tenant tenant, int userId);
        Task RemoveAsync(Tenant tenant, int userId);
    }
}
