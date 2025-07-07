using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class UserProjectRepository : JoinRepositroy<UserProject>, IUserProjectRepository
    {
        private readonly AppDbContext _db;
        private readonly INotificationRepository _notificationRepository;
        private readonly IIssueAssignUserRepository _issueAssignUserRepository;

        public UserProjectRepository(AppDbContext db, INotificationRepository notificationRepository, IIssueAssignUserRepository issueAssignUserRepository)
            : base(db)
        {
            _db = db;
            _notificationRepository = notificationRepository;
            _issueAssignUserRepository = issueAssignUserRepository;
        }

        public async Task JoinAsync(UserProject entity, int userId)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == entity.ProjectId);
            if (project == null) return;

            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == project.TenantId);
            if (tenant == null) return;

            var joiningUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId);
            if (joiningUser == null) return;

            await _db.UserProjects.AddAsync(entity);
            await _db.SaveChangesAsync();

            string userMessage = userId == project.CreatorId
                ? $"You have created a new Project: {project.Name} in Tenant: {tenant.Name}"
                : $"You have joined a new Project: {project.Name} in Tenant: {tenant.Name}";

            await _notificationRepository.SendNotificationAsync(userId, userMessage, null);

            if (userId != project.CreatorId)
            {
                string projectOwnerMsg = $"User '{joiningUser.UserName}' (Email: {joiningUser.Email}) has joined your Project: {project.Name}";
                await _notificationRepository.SendNotificationAsync(project.CreatorId, projectOwnerMsg, null);
            }

            if (userId != tenant.OwnerID && tenant.OwnerID != project.CreatorId)
            {
                string tenantOwnerMsg = $"User '{joiningUser.UserName}' (Email: {joiningUser.Email}) has joined a project '{project.Name}' under your Tenant: {tenant.Name}";
                await _notificationRepository.SendNotificationAsync(tenant.OwnerID, tenantOwnerMsg, null);
            }
        }

        public async Task LeaveAsync(UserProject entity, int userId)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == entity.ProjectId);
            if (project == null) return;

            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == project.TenantId);
            if (tenant == null) return;

            var leavingUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId);
            if (leavingUser == null) return;

            var assignedIssues = await _db.IssueAssignedUsers
                .Where(iau => iau.UserId == userId && _db.Issues.Any(issue => issue.Id == iau.IssueId && issue.ProjectId == entity.ProjectId))
                .ToListAsync();

            foreach (var issue in assignedIssues)
            {
                await _issueAssignUserRepository.LeaveAsync(issue, userId);
            }

            _db.UserProjects.Remove(entity);
            await _db.SaveChangesAsync();

            string userMessage = userId == project.CreatorId
                ? $"Project: {project.Name} in Tenant {tenant.Name} is Removed successfully"
                : $"You left the project: {project.Name} in tenant {tenant.Name} successfully";

            await _notificationRepository.SendNotificationAsync(userId, userMessage, null);

            if (userId != project.CreatorId)
            {
                string projectOwnerMsg = $"User '{leavingUser.UserName}' (Email: {leavingUser.Email}) has left your Project: {project.Name}";
                await _notificationRepository.SendNotificationAsync(project.CreatorId, projectOwnerMsg, null);
            }

            if (userId != tenant.OwnerID && tenant.OwnerID != project.CreatorId)
            {
                string tenantOwnerMsg = $"User '{leavingUser.UserName}' (Email: {leavingUser.Email}) has left the project '{project.Name}' in your Tenant: {tenant.Name}";
                await _notificationRepository.SendNotificationAsync(tenant.OwnerID, tenantOwnerMsg, null);
            }
        }

        public async Task InviteByEmailAsync(string email, int projectId, string role)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("This user doesn't use the app.");

            // Check if already joined
            bool alreadyJoined = await _db.UserProjects
                .AnyAsync(up => up.UserId == user.Id && up.ProjectId == projectId && up.AcceptedInvitation);
            if (alreadyJoined)
                throw new Exception("User already joined this project.");

            // Check if already invited
            bool alreadyInvited = await _db.UserProjects
                .AnyAsync(up => up.UserId == user.Id && up.ProjectId == projectId && !up.AcceptedInvitation);
            if (alreadyInvited)
                throw new Exception("User has already been invited but hasn't accepted yet.");

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

            // Optional: notify user about invitation
            var project = await _db.Projects.FindAsync(projectId);
            if (project != null)
            {
                var message = $"You have been invited to join the Project: {project.Name}. Please accept the invitation.";
                await _notificationRepository.SendNotificationAsync(user.Id, message, null);
            }
        }

        public async Task AcceptInvitationAsync(string email, int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Invalid user.");

            var record = await _db.UserProjects
                .Include(up => up.Project)
                .ThenInclude(p => p.Tenant)
                .FirstOrDefaultAsync(up => up.UserId == user.Id && !up.AcceptedInvitation);

            if (record == null)
                throw new Exception("No pending invitation found.");

            record.AcceptedInvitation = true;
            await _db.SaveChangesAsync();

            // Notify user
            var msg = $"You have successfully joined the Project: {record.Project.Name} in Tenant: {record.Project.Tenant.Name}";
            await _notificationRepository.SendNotificationAsync(userId, msg, null);

            // Notify project owner
            if (record.Project.CreatorId != userId)
            {
                string projectOwnerMessage = $"User '{user.UserName}' (Email: {user.Email}) has joined your Project: {record.Project.Name}";
                await _notificationRepository.SendNotificationAsync(record.Project.CreatorId, projectOwnerMessage, null);
            }

            // Notify tenant owner
            if (record.Project.Tenant.OwnerID != userId && record.Project.Tenant.OwnerID != record.Project.CreatorId)
            {
                string tenantOwnerMessage = $"User '{user.UserName}' (Email: {user.Email}) has joined a Project '{record.Project.Name}' under your Tenant: {record.Project.Tenant.Name}";
                await _notificationRepository.SendNotificationAsync(record.Project.Tenant.OwnerID, tenantOwnerMessage, null);
            }
        }
    }

}
