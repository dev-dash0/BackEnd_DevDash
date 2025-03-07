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

            var userAssignedIssuesToRemove = await _db.IssueAssignedUsers
                .Where(u => _db.Issues.Where(i => i.TenantId == tenant.Id)
                .Select(i => i.Id)
                .Contains(u.IssueId))
                .ToListAsync();

            _db.IssueAssignedUsers.RemoveRange(userAssignedIssuesToRemove);

            var issuesToRemove = await _db.Issues.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.Issues.RemoveRange(issuesToRemove);

            var sprintsToRemove = await _db.Sprints.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.Sprints.RemoveRange(sprintsToRemove);

            var userProjectsToRemove = await _db.UserProjects
                .Where(up => _db.Projects.Where(p => p.TenantId == tenant.Id)
                .Select(p => p.Id)
                .Contains(up.ProjectId))
                .ToListAsync();

            _db.UserProjects.RemoveRange(userProjectsToRemove);

            var projectsToRemove = await _db.Projects.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.Projects.RemoveRange(projectsToRemove);

            var userTenantsToRemove = await _db.UserTenants.Where(u => u.TenantId == tenant.Id).ToListAsync();
            _db.UserTenants.RemoveRange(userTenantsToRemove);

            _db.Tenants.Remove(tenant);

            await _db.SaveChangesAsync();


        }


    }
}
