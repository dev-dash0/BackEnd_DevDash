using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DevDash.Repository
{
    public class PinnedItemRepository : JoinRepositroy<PinnedItem>, IPinnedItemRepository
    {
        private readonly AppDbContext _db;
        public PinnedItemRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<PinnedItem>> GetAllAsync(Expression<Func<PinnedItem, bool>>? filter = null)
        {
            IQueryable<PinnedItem> query = _db.PinnedItems;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync();
        }

        public async Task LeaveAsync(PinnedItem entity)
        {
            _db.PinnedItems.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
