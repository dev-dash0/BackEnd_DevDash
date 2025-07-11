using DevDash.DTO;
using DevDash.DTO.UserTenant;
using DevDash.model;
using DevDash.Repository.IRepository;
using DevDash.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DevDash.Repository
{
    public class UserProjectRepository : JoinRepositroy<UserProject>, IUserProjectRepository
    {
        private readonly AppDbContext _db;
        private readonly INotificationRepository _notificationRepository;
        private readonly IIssueAssignUserRepository _issueAssignUserRepository;
        private readonly IEmailService _emailService;
        public UserProjectRepository(AppDbContext db, INotificationRepository notificationRepository,
            IEmailService emailService,
            IIssueAssignUserRepository issueAssignUserRepository)
            : base(db)
        {
            _db = db;

            _notificationRepository = notificationRepository;
            _issueAssignUserRepository = issueAssignUserRepository;
        }

        public Task CreateAsync(UserProject entity)
        {
            throw new NotImplementedException();
        }

        public async Task<List<UserProject>> GetAllAsync(Expression<Func<UserProject, bool>>? filter = null, string? includeProperties = null, int pageSize = 0, int pageNumber = 1)
        {
            var userproject =await _db.UserProjects.ToListAsync();
            return userproject;
        }

        public Task<UserProject> GetAsync(Expression<Func<UserProject, bool>>? filter = null, bool tracked = true, string? includeProperties = null)
        {
            throw new NotImplementedException();
        }

        public async Task JoinAsync(UserProject entity, int userId)
        {
            Project? project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == entity.ProjectId);
            if (project == null) return;

            Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(p => p.Id == project.TenantId);
            if (project == null) return;

            await _db.UserProjects.AddAsync(entity);
            await _db.SaveChangesAsync();

            string message = userId == project.CreatorId
                ? $"You have created a new Project: {project.Name} in Tenant {tenant.Name}"
                : $"You have joined a new Project: {project.Name} in Tenant {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, message,null);
        }

        public async Task LeaveAsync(UserProject entity, int userId)
        {
            Project? project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == entity.ProjectId);
            if (project == null) return;

            Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(p => p.Id == project.TenantId);
            if (project == null) return;

            var assignedIssues = await _db.IssueAssignedUsers
                .Where(iau => iau.UserId == userId && _db.Issues.Any(issue => issue.Id == iau.IssueId && issue.ProjectId == entity.ProjectId))
                .ToListAsync();

            foreach (var issue in assignedIssues)
            {
                await _issueAssignUserRepository.LeaveAsync(issue, userId);
            }

            _db.UserProjects.Remove(entity);
            await _db.SaveChangesAsync();

            string message = userId == project.CreatorId
                ? $"Project: {project.Name} in Tenant {tenant.Name} is Removed successfully"
                : $"You left the project: {project.Name} in tenant {tenant.Name} successfully";

            await _notificationRepository.SendNotificationAsync(userId, message, null);
        }
        public async Task<string> InviteByEmailAsync(int inviterUserId, string email, int projectId, string role)
        {
            // Step 1: Load project and tenant
            var project = await _db.Projects
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new Exception("Project not found.");

            var tenantId = project.TenantId;

            // Step 2: Check if inviter has permission (Admin on tenant or Project Manager on project)
            var isTenantAdmin = await _db.UserTenants.AnyAsync(ut =>
                ut.UserId == inviterUserId &&
                ut.TenantId == tenantId &&
                ut.AcceptedInvitation &&
                ut.Role.ToLower() == "admin");

            var isProjectManager = await _db.UserProjects.AnyAsync(up =>
                up.UserId == inviterUserId &&
                up.ProjectId == projectId &&
                up.AcceptedInvitation &&
                up.Role.ToLower() == "project manager");

            if (!isTenantAdmin && !isProjectManager)
                throw new Exception("Only tenant Admins or project Managers can send invitations to this project.");

            // Step 3: Find invited user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found or doesn't use the app.");

            // Step 4: Check if user is already in the project
            var existingRecord = await _db.UserProjects
                .FirstOrDefaultAsync(up => up.UserId == user.Id && up.ProjectId == projectId);

            if (existingRecord != null)
            {
                if (!existingRecord.AcceptedInvitation)
                {
                    await SendProjectInvitationEmailAsync(user, project);
                    return $"Invitation re-sent to {user.UserName}.";
                }

                return $"{user.UserName} is already a member of this project.";
            }

            // Step 5: Create and send new invitation
            var invitation = new UserProject
            {
                UserId = user.Id,
                ProjectId = projectId,
                Role = role,
                AcceptedInvitation = false,
                JoinedDate = DateTime.UtcNow
            };

            await _db.UserProjects.AddAsync(invitation);
            await _db.SaveChangesAsync();

            await SendProjectInvitationEmailAsync(user, project);
            return $"Invitation sent to {user.UserName}.";
        }
        private async Task SendProjectInvitationEmailAsync(User user, Project project)
        {
            var baseUrl = "http://devdash.runasp.net/api/UserProject/accept";
            var invitationLink = $"{baseUrl}?email={Uri.EscapeDataString(user.Email)}&projectId={project.Id}";

            var subject = $"Invitation to join project '{project.Name}' in tenant '{project.Tenant.Name}'";
            var body = $@"
        <p>Hello {user.UserName},</p>
        <p>You have been invited to join the project <strong>{project.Name}</strong> under the tenant <strong>{project.Tenant.Name}</strong>.</p>
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
        public async Task<string> AcceptInvitationAsync(string email, int projectId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return "Invalid user.";

            var record = await _db.UserProjects
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.ProjectId == projectId);

            if (record == null)
                return "Join failed. Invitation not found.";

            if (record.AcceptedInvitation)
                return $"You are already a member of this project.";

            record.AcceptedInvitation = true;
            await _db.SaveChangesAsync();

            var project = await _db.Projects.FindAsync(record.ProjectId);
            var tenant = await _db.Tenants.FindAsync(project.TenantId);
            if (project != null)
            {
                var msg = $"You have successfully joined the tenant: {project.Name}";
                var msg2 = $"User {user.UserName} have successfully joined the tenant: {project.Name}";
                await _notificationRepository.SendNotificationAsync(user.Id, msg, null);
                await _notificationRepository.SendNotificationAsync(tenant.OwnerID, msg2, null);

            }

            return "Invitation accepted successfully.";
        }
        public async Task<string> UpdateUserRoleAsync(int inviterUserId, int projectId, UpdateUserRoleDto dto)
        {
            var project = await _db.Projects.Include(p => p.Tenant).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                throw new Exception("Project not found.");

            var tenantId = project.TenantId;

            // Check inviter permissions
            var isTenantAdmin = await _db.UserTenants.AnyAsync(ut =>
                ut.UserId == inviterUserId &&
                ut.TenantId == tenantId &&
                ut.AcceptedInvitation &&
                ut.Role == "Admin");

            var isProjectManager = await _db.UserProjects.AnyAsync(up =>
                up.UserId == inviterUserId &&
                up.ProjectId == projectId &&
                up.AcceptedInvitation &&
                up.Role == "Project Manager");

            if (!isTenantAdmin && !isProjectManager)
                throw new Exception("Only Admins or Project Managers can change roles in this project.");

            // Find target user record
            var targetRecord = await _db.UserProjects.FirstOrDefaultAsync(up =>
                up.ProjectId == projectId && up.UserId == dto.UserId);

            if (targetRecord == null)
                throw new Exception("User not found in the project.");

            if (!targetRecord.AcceptedInvitation)
                throw new Exception("User hasn't accepted the invitation yet.");

            // Update role
            targetRecord.Role = dto.NewRole;
            await _db.SaveChangesAsync();

            return $"Role updated to '{dto.NewRole}' for user ID {dto.UserId} in project ID {projectId}.";
        }
    }
}
