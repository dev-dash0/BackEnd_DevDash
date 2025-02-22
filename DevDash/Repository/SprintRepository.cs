using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class SprintRepository : Repository<Sprint>, ISprintRepository
    {
        private readonly AppDbContext _db;
        public SprintRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }
        public async Task RemoveAsync(Sprint sprint)
        {
            var commentsToRemove = await _db.Comments.Where(u => u.SprintId == sprint.Id).ToListAsync();
            _db.Comments.RemoveRange(commentsToRemove);
            var IssuesToRemove = await _db.Issues.Where(u => u.SprintId == sprint.Id).ToListAsync();
            _db.Issues.RemoveRange(IssuesToRemove);
            _db.Sprints.Remove(sprint);
            await _db.SaveChangesAsync();
        }

        public async Task<Sprint> UpdateAsync(Sprint sprint)
        {
            _db.Sprints.Update(sprint);
            await _db.SaveChangesAsync();
            return sprint;
        }
    }
}
