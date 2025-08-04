using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface IGitHubIntegrationRepository: IRepository<GitHubIntegration>
    {
        Task<GitHubIntegration?> GetByUserIdAsync(string userId);
        Task UpdateAsync(GitHubIntegration integration);
    }
}
