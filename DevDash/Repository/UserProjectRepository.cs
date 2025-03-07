using DevDash.model;
using DevDash.Repository.IRepository;

namespace DevDash.Repository
{
    public class UserProjectRepository : JoinRepositroy<UserProject>, IUserProjectRepository
    {
        private readonly AppDbContext _db;
        public UserProjectRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task LeaveAsync(UserProject entity)
        {

            _db.UserProjects.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
