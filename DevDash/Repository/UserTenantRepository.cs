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

        public async Task JoinAsync(UserTenant entity, int userId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == entity.TenantId);
            if (tenant == null) return;

            var joiningUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId);
            if (joiningUser == null) return;

            await _db.UserTenants.AddAsync(entity);
            await _db.SaveChangesAsync();

            string userMessage = userId == tenant.OwnerID
                ? $"You have created a new Tenant: {tenant.Name}"
                : $"You have joined a new Tenant: {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, userMessage, null);

            // Notify Tenant Owner if different from user
            if (userId != tenant.OwnerID)
            {
                string ownerMessage = $"User '{joiningUser.UserName}' (Email: {joiningUser.Email}) has joined your Tenant: {tenant.Name}";
                await _notificationRepository.SendNotificationAsync(tenant.OwnerID, ownerMessage, null);
            }
        }

        public async Task LeaveAsync(UserTenant entity, int userId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == entity.TenantId);
            if (tenant == null) return;

            var leavingUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId);
            if (leavingUser == null) return;

            // Leave all related projects
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

            string userMessage = userId == tenant.OwnerID
                ? $"Tenant '{tenant.Name}' has been removed successfully."
                : $"You have left the Tenant: {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, userMessage, null);

            // Notify Tenant Owner if different from user
            if (userId != tenant.OwnerID)
            {
                string ownerMessage = $"User '{leavingUser.UserName}' (Email: {leavingUser.Email}) has left your Tenant: {tenant.Name}";
                await _notificationRepository.SendNotificationAsync(tenant.OwnerID, ownerMessage, null);
            }
        }

        public async Task InviteByEmailAsync(string email, int tenantId, string role)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("This user doesn't use the app.");

            // Check if already joined
            bool alreadyJoined = await _db.UserTenants
                .AnyAsync(ut => ut.UserId == user.Id && ut.TenantId == tenantId && ut.AcceptedInvitation);
            if (alreadyJoined)
                throw new Exception("User already joined this tenant.");

            // Check if already invited and not accepted yet
            bool alreadyInvited = await _db.UserTenants
                .AnyAsync(ut => ut.UserId == user.Id && ut.TenantId == tenantId && !ut.AcceptedInvitation);
            if (alreadyInvited)
                throw new Exception("User has already been invited but hasn't accepted yet.");

            var invitation = new UserTenant
            {
                UserId = user.Id,
                TenantId = tenantId,
                Role = role,
                AcceptedInvitation = false,
                JoinedDate = DateTime.UtcNow
            };

            await _db.UserTenants.AddAsync(invitation);
            await _db.SaveChangesAsync();

            // Optionally notify the user
            var tenant = await _db.Tenants.FindAsync(tenantId);
            if (tenant != null)
            {
                string message = $"You have been invited to join the Tenant: {tenant.Name}. Please accept the invitation.";
                await _notificationRepository.SendNotificationAsync(user.Id, message, null);
            }
        }

        public async Task AcceptInvitationAsync(string email, int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Invalid user.");

            var record = await _db.UserTenants
                .Include(ut => ut.Tenant)
                .FirstOrDefaultAsync(ut => ut.UserId == user.Id && !ut.AcceptedInvitation);
            if (record == null)
                throw new Exception("No pending invitation found.");

            record.AcceptedInvitation = true;
            await _db.SaveChangesAsync();

            // Notify user
            var userMsg = $"You have successfully joined the Tenant: {record.Tenant.Name}";
            await _notificationRepository.SendNotificationAsync(userId, userMsg, null);

            // Notify tenant owner
            var ownerMsg = $"User '{user.UserName}' (Email: {user.Email}) has accepted your invitation and joined Tenant: {record.Tenant.Name}";
            await _notificationRepository.SendNotificationAsync(record.Tenant.OwnerID, ownerMsg, null);
        }

    }
}
