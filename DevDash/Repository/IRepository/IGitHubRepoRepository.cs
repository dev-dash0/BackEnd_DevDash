using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface IGitHubRepoRepository: IRepository<GitHubRepository>
    {
        Task<GitHubRepository?> GetByUserIdAndRepoNameAsync(string userId, string repoName);
        Task<List<GitHubRepository>> GetByUserIdAsync(string userId, int pageSize = 0, int pageNumber = 1);
        Task UpdateAsync(GitHubRepository repository);
        Task DeleteAsync(GitHubRepository repository);
    }
}
