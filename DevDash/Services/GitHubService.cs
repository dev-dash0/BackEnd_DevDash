using AutoMapper;
using DevDash.DTO.Integrations.Github;
using DevDash.model;
using DevDash.Repository.IRepository;
using DevDash.Services.IService;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DevDash.Services
{
    public class GitHubService :IGitHubService
    {
        private readonly IConfiguration _config;
        private readonly IGitHubIntegrationRepository _integrationRepo;
        private readonly IGitHubRepoRepository _repoRepo;
        private readonly IMemoryCache _cache;
        private readonly HttpClient _http;
        private readonly IMapper _mapper;

        public GitHubService(IConfiguration config,
        IGitHubIntegrationRepository integrationRepo,
        IGitHubRepoRepository repoRepo,
        IMemoryCache cache,
        IHttpClientFactory factory,
        IMapper mapper)
        {
            _config = config;
            _integrationRepo = integrationRepo;
            _repoRepo = repoRepo;
            _cache = cache;
            _http = factory.CreateClient("GitHubClient");
            _mapper = mapper;
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("DevDash");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        public string GetOAuthRedirectUrl(string state, EnableGithubRequestsDTO dto, string userId)
        {
            _cache.Set($"gh-state-user", (userId, dto.DesiredRepoName,dto.GitHubEmailOrProfile), TimeSpan.FromMinutes(10));
            return $"https://github.com/login/oauth/authorize?client_id={_config["Authentication:GitHub:ClientId"]}&scope=public_repo&redirect_uri={_config["Authentication:GitHub:CallbackRoot"]}/api/github/callback&state=user";
        }

        public async Task HandleOAuthCallbackAsync(string code, string state)
        {
            if (!_cache.TryGetValue<(string userId, string repoName, string emailOrProfile)>($"gh-state-user", out var data))
                throw new Exception("Invalid state");

            var (userId, repoName,emailOrProfile) = data;
            var token = await ExchangeCodeForToken(code);

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var content = JsonContent.Create(new { name = repoName, @private = false });
            var response = await _http.PostAsync("https://api.github.com/user/repos", content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = json;
                try
                {
                    var errorJson = JsonDocument.Parse(errorContent).RootElement;
                    var message = errorJson.GetProperty("message").GetString();
                    throw new Exception($"GitHub repo creation failed: {message}");
                }
                catch
                {
                    throw new Exception($"GitHub repo creation failed: {errorContent}");
                }
            }

            var doc = JsonDocument.Parse(json).RootElement;

            var existing = await _integrationRepo.GetByUserIdAsync(userId);
            if (existing == null)
            {
                await _integrationRepo.CreateAsync(new GitHubIntegration
                {
                    UserId = userId,
                    AccessToken = token,
                    ConnectedAt = DateTime.UtcNow,
                    IsEnabled = true,
                    GitHubEmailOrProfile= emailOrProfile,
                });
            }
            else
            {
                existing.AccessToken = token;
                existing.IsEnabled = true;
                existing.ConnectedAt = DateTime.UtcNow;
                await _integrationRepo.UpdateAsync(existing);
            }


            await _repoRepo.CreateAsync(new GitHubRepository
            {
                UserId = userId,
                RepositoryName = doc.GetProperty("name").GetString()!,
                RepositoryUrl = doc.GetProperty("html_url").GetString()!,
                CreatedAt = doc.GetProperty("created_at").GetDateTime(),
                IsPublic = !doc.GetProperty("private").GetBoolean()
            });
        }

        private async Task<string> ExchangeCodeForToken(string code)
        {
            var data = new Dictionary<string, string>
            {
                ["client_id"] = _config["Authentication:GitHub:ClientId"]!,
                ["client_secret"] = _config["Authentication:GitHub:ClientSecret"]!,
                ["code"] = code,
                ["redirect_uri"] = $"{_config["Authentication:GitHub:CallbackRoot"]}/api/github/callback"
            };

            var response = await _http.PostAsync("https://github.com/login/oauth/access_token", new FormUrlEncodedContent(data));
            var result = await response.Content.ReadAsStringAsync();
            //var accessToken = System.Web.HttpUtility.ParseQueryString(result)["access_token"];
            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to obtain GitHub access token.");
            try
            {
                var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(result);
                if (tokenResponse != null && tokenResponse.TryGetValue("access_token", out var token))
                {
                    return token!;
                }
            }
            catch
            {
                var accessToken = System.Web.HttpUtility.ParseQueryString(result)["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                    return accessToken!;
            }
            throw new Exception("Failed to obtain GitHub access token. Raw response: " + result);
        }

        public async Task DisableAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty");
            var integration = await _integrationRepo.GetAsync(i => i.UserId == userId);
            if (integration != null)
            {
                integration.IsEnabled = false;
                integration.AccessToken = null;
                await _integrationRepo.UpdateAsync(integration);
            }
        }

        public async Task<List<GitHubRepoDTO>> GetUserReposAsync(string userId)
        {
            var repos = await _repoRepo.GetByUserIdAsync(userId);
            return _mapper.Map<List<GitHubRepoDTO>>(repos);
        }
    }
}
