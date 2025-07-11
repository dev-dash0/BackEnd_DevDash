using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DevDash.Repository
{
    public class UserTenantRepository : JoinRepositroy<UserTenant>, IUserTenantRepository
    {
        private readonly AppDbContext _db;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserProjectRepository _userProjectRepository;

        public UserTenantRepository(AppDbContext db, INotificationRepository notificationRepository, IUserProjectRepository userProjectRepository)
            : base(db)
        {
            _db = db;
            _notificationRepository = notificationRepository;
            _userProjectRepository = userProjectRepository;
        }

        public Task CreateAsync(UserTenant entity)
        {
            throw new NotImplementedException();
        }

        public async Task<List<UserTenant>> GetAllAsync(Expression<Func<UserTenant, bool>>? filter = null, string? includeProperties = null, int pageSize = 0, int pageNumber = 1)
        {
            var usertenant=await _db.UserTenants.Include(ut => ut.Tenant).Include(ut => ut.User).ToListAsync();
            return usertenant;
        }

        public Task<UserTenant> GetAsync(Expression<Func<UserTenant, bool>>? filter = null, bool tracked = true, string? includeProperties = null)
        {
            throw new NotImplementedException();
        }

        public async Task JoinAsync(UserTenant entity, int userId)
        {
            Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == entity.TenantId);
            if (tenant == null) return;

            await _db.UserTenants.AddAsync(entity);
            await _db.SaveChangesAsync();

            string message = userId == tenant.OwnerID
                ? $"You have created a new Tenant: {tenant.Name}"
                : $"You have joined a new Tenant: {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, message, null);
        }

        public async Task LeaveAsync(UserTenant entity, int userId)
        {
            Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == entity.TenantId);
            if (tenant == null) return;

            var projectIds = await _db.Projects
                .Where(p => p.TenantId == entity.TenantId)
                .Select(p => p.Id)
                .ToListAsync();

            var userProjects = await _db.UserProjects
                .Where(up => up.UserId == entity.UserId && projectIds.Contains(up.ProjectId))
                .ToListAsync();

            foreach (var userProject in userProjects)
            {
                await _userProjectRepository.LeaveAsync(userProject, userId);
            }

            _db.UserTenants.Remove(entity);
            await _db.SaveChangesAsync();

            string message = userId == tenant.OwnerID
                ? $"Tenant '{tenant.Name}' has been removed successfully."
                : $"You have left the Tenant: {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, message, null);
        }
    }
}
