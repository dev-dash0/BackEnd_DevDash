using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;

namespace DevDash.Repository
{
    public class GitHubRepoRepository: IGitHubRepoRepository
    {
        private readonly AppDbContext _db;
        public GitHubRepoRepository(AppDbContext db)
        {
            _db = db;
        }
        public async Task CreateAsync(GitHubRepository entity)
        {
            _db.GitHubRepositories.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(GitHubRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository), "Repository cannot be null");
            }
            _db.GitHubRepositories.Remove(repository);
            await _db.SaveChangesAsync();
        }

        public Task<List<GitHubRepository>> GetAllAsync(Expression<Func<GitHubRepository, bool>>? filter = null, string? includeProperties = null, int pageSize = 0, int pageNumber = 1)
        {
            IQueryable<GitHubRepository> query = _db.GitHubRepositories;
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

        public Task<GitHubRepository?> GetAsync(Expression<Func<GitHubRepository, bool>>? filter = null, bool tracked = true, string? includeProperties = null)
        {
            IQueryable<GitHubRepository> query = _db.GitHubRepositories;
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

        public Task<GitHubRepository?> GetByUserIdAndRepoNameAsync(string userId, string repoName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(repoName))
            {
                throw new ArgumentException("UserId and RepoName cannot be null or empty");
            }
            return _db.GitHubRepositories
                .FirstOrDefaultAsync(repo => repo.UserId == userId && repo.RepositoryName == repoName);
        }

        public Task<List<GitHubRepository>> GetByUserIdAsync(string userId, int pageSize = 0, int pageNumber = 1)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
            }
            IQueryable<GitHubRepository> query = _db.GitHubRepositories.Where(repo => repo.UserId == userId);
            if (pageSize > 0)
            {
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }
            return query.ToListAsync();
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(GitHubRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository), "Repository cannot be null");
            }
            _db.GitHubRepositories.Update(repository);
            await _db.SaveChangesAsync();
        }
    }
}
