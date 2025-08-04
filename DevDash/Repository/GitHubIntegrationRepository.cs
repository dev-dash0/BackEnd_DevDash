using DevDash.model;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;
using DevDash.Repository.IRepository;

namespace DevDash.Repository
{
    public class GitHubIntegrationRepository : IGitHubIntegrationRepository
    {
        private readonly AppDbContext _db;
        public GitHubIntegrationRepository(AppDbContext db)
        {
            _db = db;
        }
        public async Task CreateAsync(GitHubIntegration entity)
        {
            _db.GitHubIntegration.Add(entity);
            await _db.SaveChangesAsync();
        }

        public Task<List<GitHubIntegration>> GetAllAsync(Expression<Func<GitHubIntegration, bool>>? filter = null, string? includeProperties = null, int pageSize = 0, int pageNumber = 1)
        {
            IQueryable<GitHubIntegration> query = _db.GitHubIntegration;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }
            if (pageSize > 0)
            {
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }
            return query.ToListAsync();
        }

        public Task<GitHubIntegration?> GetAsync(Expression<Func<GitHubIntegration, bool>>? filter = null, bool tracked = true, string? includeProperties = null)
        {
            IQueryable<GitHubIntegration> query = _db.GitHubIntegration;
            if (!tracked)
            {
                query = query.AsNoTracking();
            }
            if (filter != null)
            {
                query = query.Where(filter);
            }
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }
            return query.FirstOrDefaultAsync();
        }

        public async Task<GitHubIntegration?> GetByUserIdAsync(string userId)
        {
            return await _db.GitHubIntegration.FirstOrDefaultAsync(g => g.UserId == userId);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(GitHubIntegration integration)
        {
            _db.GitHubIntegration.Update(integration);
            await _db.SaveChangesAsync();
        }
    }
}
