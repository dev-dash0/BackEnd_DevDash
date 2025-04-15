using DevDash.Repository.IRepository;

namespace DevDash.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public IProjectRepository Project { get; private set; }

        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            Project = new ProjectRepository(_db);
        }
        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
