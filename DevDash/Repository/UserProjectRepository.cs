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
            Project? project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == entity.ProjectId);
            if (project == null) return;

            await _db.UserProjects.AddAsync(entity);
            await _db.SaveChangesAsync();

            string message = userId == project.CreatorId
                ? $"You have created a new Project: {project.Name}"
                : $"You have joined a new Project: {project.Name}";

            await _notificationRepository.SendNotificationAsync(userId, message);
        }

        public async Task LeaveAsync(UserProject entity, int userId)
        {
            Project? project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == entity.ProjectId);
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
                ? $"Project: {project.Name} is Removed Successfully"
                : $"You left the project: {project.Name}";

            await _notificationRepository.SendNotificationAsync(userId, message);
        }
    }
}
