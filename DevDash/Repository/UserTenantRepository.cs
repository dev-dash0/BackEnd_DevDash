using DevDash.DTO;
using DevDash.DTO.UserTenant;
using DevDash.model;
using DevDash.Repository.IRepository;
using DevDash.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DevDash.Repository
{
    public class UserTenantRepository : JoinRepositroy<UserTenant>, IUserTenantRepository
    {
        private readonly AppDbContext _db;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserProjectRepository _userProjectRepository;
        private readonly IEmailService _emailService;
        public UserTenantRepository(AppDbContext db, INotificationRepository notificationRepository,
            IEmailService emailService
            , IUserProjectRepository userProjectRepository)
            : base(db)
        {
            _db = db;
            _emailService = emailService;
            _notificationRepository = notificationRepository;
            _userProjectRepository = userProjectRepository;
        }

        public Task CreateAsync(UserTenant entity)
        {
            throw new NotImplementedException();
        }

        public async Task<List<UserTenant>> GetAllAsync(
      Expression<Func<UserTenant, bool>>? filter = null,
      string? includeProperties = null,
      int pageSize = 0,
      int pageNumber = 1)
        {
            IQueryable<UserTenant> query = _db.UserTenants;

            // Apply filter
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Include related entities
            query = query.Include(ut => ut.Tenant).Include(ut => ut.User);

            // Apply paging
            if (pageSize > 0)
            {
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }

            return await query.ToListAsync();
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
        public async Task<string> InviteByEmailAsync(int inviterUserId, string email, int tenantId, string role)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
                throw new Exception("Tenant not found.");

            // Step 1: Ensure inviter is part of the tenant
            var inviterTenantRecord = await _db.UserTenants
                .FirstOrDefaultAsync(ut => ut.UserId == inviterUserId && ut.TenantId == tenantId);

            if (inviterTenantRecord == null || !inviterTenantRecord.AcceptedInvitation)
                throw new Exception("You are not a member of this tenant.");

            // Step 2: Ensure inviter has Admin role
            if (!string.Equals(inviterTenantRecord.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only Admins can send invitations to this tenant.");

            // Step 3: Check if invited user exists
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found or doesn't use the app.");


            var existingRecord = await _db.UserTenants
                .FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TenantId == tenantId);

            if (existingRecord != null)
            {
                if (!existingRecord.AcceptedInvitation)
                {
                    // Re-send invitation
                    await SendInvitationEmailAsync(user, tenant);
                    return $"Invitation re-sent to {user.UserName}.";
                }
                else
                {
                    return $"{user.UserName} is already a member of this tenant.";
                }
            }

            // Step 4: Create new invitation
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

            await SendInvitationEmailAsync(user, tenant);
            return $"Invitation sent to {user.UserName}.";
        }
        private async Task SendInvitationEmailAsync(User user, Tenant tenant)
        {
            var baseUrl = "http://devdash.runasp.net/api/UserTenant/accept";
            var invitationLink = $"{baseUrl}?email={Uri.EscapeDataString(user.Email)}&tenantId={tenant.Id}";

            var subject = "Invitation to join a Tenant in DevDash";
            var body = $@"
        <p>Hello {user.UserName},</p>
        <p>You have been invited to join tenant '<strong>{tenant.Name}</strong>' in <strong>DevDash</strong>.</p>
        <p>Click <a href=""{invitationLink}"">here</a> to accept the invitation.</p>
        <br/>
        <p>If you did not expect this invitation, please ignore it.</p>";

            var emailDto = new EmailDto
            {
                To = user.Email,
                Subject = subject,
                Body = body
            };

            await _emailService.SendAsync(emailDto);
        }
        public async Task<string> AcceptInvitationAsync(string email, int tenantId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return "Invalid user.";

            var record = await _db.UserTenants
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.TenantId == tenantId);

            if (record == null)
                return "Join failed. Invitation not found.";

            if (record.AcceptedInvitation)
                return $"You are already a member of this tenant.";

            record.AcceptedInvitation = true;
            await _db.SaveChangesAsync();

            var tenant = await _db.Tenants.FindAsync(record.TenantId);
            if (tenant != null)
            {
                var msg = $"You have successfully joined the tenant: {tenant.Name}";
                var msg2 = $"User {user.UserName} have successfully joined the tenant: {tenant.Name}";
                await _notificationRepository.SendNotificationAsync(user.Id, msg, null);
                await _notificationRepository.SendNotificationAsync(tenant.OwnerID, msg, null);
            }

            return "Invitation accepted successfully.";
        }
        public async Task<string> UpdateUserRoleAsync(int inviterUserId, int tenantId, UpdateUserRoleDto dto)
        {
            // Ensure the inviter is Admin in the tenant
            var inviterRecord = await _db.UserTenants.FirstOrDefaultAsync(ut =>
                ut.TenantId == tenantId &&
                ut.UserId == inviterUserId &&
                ut.AcceptedInvitation &&
                ut.Role == "Admin");

            if (inviterRecord == null)
                throw new Exception("Only Admins can change roles in this tenant.");

            // Find the user to be updated
            var targetRecord = await _db.UserTenants.FirstOrDefaultAsync(ut =>
                ut.TenantId == tenantId && ut.UserId == dto.UserId);

            if (targetRecord == null)
                throw new Exception("User not found in the tenant.");

            if (!targetRecord.AcceptedInvitation)
                throw new Exception("User hasn't accepted the invitation yet.");

            // Update role
            targetRecord.Role = dto.NewRole;
            await _db.SaveChangesAsync();

            return $"Role updated to '{dto.NewRole}' for user ID {dto.UserId} in tenant ID {tenantId}.";
        }

    }
}
