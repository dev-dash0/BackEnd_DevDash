using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

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

        public async Task JoinAsync(UserTenant entity, string userId)
        {
            Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == entity.TenantId);
            if (tenant == null) return;

            await _db.UserTenants.AddAsync(entity);
            await _db.SaveChangesAsync();

            string message = userId == tenant.OwnerID.ToString()
                ? $"You have created a new Tenant: {tenant.Name}"
                : $"You have joined a new Tenant: {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, message);
        }

        public async Task LeaveAsync(UserTenant entity, string userId)
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

            string message = userId == tenant.OwnerID.ToString()
                ? $"Tenant '{tenant.Name}' has been removed successfully."
                : $"You have left the Tenant: {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, message);
        }
    }
}
