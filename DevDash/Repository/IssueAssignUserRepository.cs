using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class IssueAssignUserRepository : JoinRepositroy<IssueAssignedUser>, IIssueAssignUserRepository
    {
        private readonly AppDbContext _db;
        private readonly INotificationRepository _notificationRepository;

        public IssueAssignUserRepository(AppDbContext db, INotificationRepository notificationRepository) : base(db)
        {
            _db = db;
            _notificationRepository = notificationRepository;
        }

        public async Task JoinAsync(IssueAssignedUser entity, int userId)
        {
            Issue? issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == entity.IssueId);
            if (issue == null) return;

            Project? project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == issue.ProjectId);
            if (project == null) return;

            Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(p => p.Id == issue.TenantId);
            if (project == null) return;

            await _db.IssueAssignedUsers.AddAsync(entity);
            await _db.SaveChangesAsync();

            await _notificationRepository.SendNotificationAsync(userId,
                $"Issue: {issue.Title} in tenant {tenant.Name} in project {project.Name} is assigned to you",issue.Id);
        }

        public async Task LeaveAsync(IssueAssignedUser entity, int userId)
        {
            Issue? issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == entity.IssueId);
            if (issue == null) return;

            _db.IssueAssignedUsers.Remove(entity);
            await _db.SaveChangesAsync();

            await _notificationRepository.SendNotificationAsync(userId,
                $"You have left an Issue: {issue.Title}",null);
        }
    }
}
