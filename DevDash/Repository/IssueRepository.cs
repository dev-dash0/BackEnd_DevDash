using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class IssueRepository : Repository<Issue>, IIssueRepository
    {
        private AppDbContext _db;
        public IssueRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Issue> UpdateAsync(Issue issue)
        {
            issue.LastUpdate = DateTime.Now;
            _db.Issues.Update(issue);
            await _db.SaveChangesAsync();
            return issue;
        }
        public async Task RemoveAsync(Issue issue)
        {
            var commentsToRemove = await _db.Comments.Where(u => u.IssueId == issue.Id).ToListAsync();
            _db.Comments.RemoveRange(commentsToRemove);

            _db.Issues.Remove(issue);

            await _db.SaveChangesAsync();
        }

    }
}
