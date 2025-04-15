namespace DevDash.Repository.IRepository
{
    public interface IUnitOfWork
    {
        IProjectRepository Project { get; }
        void Save();
    }
}
