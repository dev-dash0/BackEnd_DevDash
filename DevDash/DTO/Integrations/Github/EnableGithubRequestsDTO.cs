using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Integrations.Github
{
    public class EnableGithubRequestsDTO
    {
        [Required]
        public string DesiredRepoName { get; set; } = string.Empty;

        [Required]
        public string GitHubEmailOrProfile { get; set; } = string.Empty;
    }
}
