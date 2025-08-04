namespace DevDash.DTO.Integrations.Github
{
    public class GitHubRepoDTO
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string? RepositoryUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsPublic { get; set; }
    }
}
