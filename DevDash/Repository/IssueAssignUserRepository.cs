using DevDash.model;
using DevDash.Repository.IRepository;
using System.Linq.Expressions;

namespace DevDash.Repository
{
    public class IssueAssignUserRepository : JoinRepositroy<IssueAssignedUser>,IIssueAssignUserRepository
    {
        private readonly AppDbContext _db;
        public IssueAssignUserRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }
        public async Task LeaveAsync(IssueAssignedUser entity)
        {
            _db.IssueAssignedUsers.Remove(entity);
            await _db.SaveChangesAsync();
        }


    }

}
