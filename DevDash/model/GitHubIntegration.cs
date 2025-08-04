namespace DevDash.model
{
    public class GitHubIntegration
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? AccessToken { get; set; }
        public DateTime ConnectedAt { get; set; }

        public string? GitHubEmailOrProfile { get; set; }
    }
}
