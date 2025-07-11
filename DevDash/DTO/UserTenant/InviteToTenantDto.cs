using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.UserTenant
{
    public class InviteToTenantDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public int TenantId { get; set; }

        [Required]
        [RegularExpression("^(Admin|Developer|Project Manager)$", ErrorMessage = "Invalid role")]
        public string Role { get; set; }
    }
}
