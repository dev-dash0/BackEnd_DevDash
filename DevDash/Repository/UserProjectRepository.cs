using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
    }
}
