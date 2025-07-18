using DevDash.model;
using System.Linq.Expressions;

namespace DevDash.Repository.IRepository
{
    public interface IPinnedItemRepository : IJoinRepository<PinnedItem>
    {
        public Task LeaveAsync(PinnedItem entity);

        Task<IEnumerable<PinnedItem>> GetAllAsync(Expression<Func<PinnedItem, bool>>? filter = null);

    }
}
