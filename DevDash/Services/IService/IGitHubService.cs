using DevDash.DTO.Integrations.Github;

namespace DevDash.Services.IService
{
    public interface IGitHubService
    {
        string GetOAuthRedirectUrl(string state, EnableGithubRequestsDTO dto, string userId);
        Task HandleOAuthCallbackAsync(string code, string state);
        Task DisableAsync(string userId);
        Task<List<GitHubRepoDTO>> GetUserReposAsync(string userId);
    }
}
