using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class UserTenantRepository : JoinRepositroy<UserTenant>, IUserTenantRepository
    {
        private readonly AppDbContext _db;
        public UserTenantRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task LeaveAsync(UserTenant entity)
        {
            var projectIds = await _db.Projects
                .Where(p => p.TenantId == entity.TenantId)
                .Select(p => p.Id)
                .ToListAsync();

            var userProjectsToRemove = await _db.UserProjects
                .Where(up => up.UserId == entity.UserId && projectIds.Contains(up.ProjectId))
                .ToListAsync();
            _db.UserProjects.RemoveRange(userProjectsToRemove);

            _db.UserTenants.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
