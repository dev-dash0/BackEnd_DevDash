namespace DevDash.model
{
    public class GitHubRepository
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public string? RepositoryUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPublic { get; set; }
    }
}
