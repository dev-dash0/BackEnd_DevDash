using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.UserProject
{
    public class InviteToProjectDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public int ProjectId { get; set; }

        [Required]
        [RegularExpression("^(Developer|Project Manager)$", ErrorMessage = "Invalid role")]
        public string Role { get; set; }
    }
}