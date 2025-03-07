using System.Linq.Expressions;

namespace DevDash.Repository.IRepository
{
    public interface IJoinRepository<T> where T : class
    {
        Task JoinAsync(T entity);
        Task<T> GetAsync(Expression<Func<T, bool>>? filter = null);
        Task SaveAsync();
    }
}
