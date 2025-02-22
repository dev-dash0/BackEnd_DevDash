using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class TenantRepository : Repository<Tenant>, ITenantRepository
    {
        private readonly AppDbContext _db;
        public TenantRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }


        public async Task<Tenant> UpdateAsync(Tenant tenant)
        {
            _db.Tenants.Update(tenant);
            await _db.SaveChangesAsync();
            return tenant;
        }
        public async Task RemoveAsync(Tenant tenant)
        {
            var commentsToRemove = await _db.Comments.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.Comments.RemoveRange(commentsToRemove);

            var IssuesToRemove = await _db.Issues.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.Issues.RemoveRange(IssuesToRemove);

            var SprintsToRemove = await _db.Sprints.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.Sprints.RemoveRange(SprintsToRemove);

            var ProjectsToRemove = await _db.Projects.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.Projects.RemoveRange(ProjectsToRemove);

            var UserTenantsToRemove = await _db.UserTenants.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.UserTenants.RemoveRange(UserTenantsToRemove);

            _db.Tenants.Remove(tenant);

            await _db.SaveChangesAsync();

        }

        
    }
}
