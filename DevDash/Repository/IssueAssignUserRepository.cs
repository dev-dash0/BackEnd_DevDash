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

        public async Task JoinAsync(IssueAssignedUser entity, string userId)
        {
            Issue? issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == entity.IssueId);
            if (issue == null) return;

            await _db.IssueAssignedUsers.AddAsync(entity);
            await _db.SaveChangesAsync();

            await _notificationRepository.SendNotificationAsync(userId,
                $"Issue: {issue.Title} is assigned to you");
        }

        public async Task LeaveAsync(IssueAssignedUser entity, string userId)
        {
            Issue? issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == entity.IssueId);
            if (issue == null) return;

            _db.IssueAssignedUsers.Remove(entity);
            await _db.SaveChangesAsync();

            await _notificationRepository.SendNotificationAsync(userId,
                $"You have left an Issue: {issue.Title}");
        }
    }
}
