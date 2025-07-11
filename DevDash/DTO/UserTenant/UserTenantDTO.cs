using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.UserTenant
{
    public class UserTenantDTO
    {
        public int UserId { get; set; }

        public int TenantId { get; set; }

        [RegularExpression("^(Admin|Developer|Project Manager)$", ErrorMessage = "Invalid role")]
        public required string Role { get; set; }
        public bool AcceptedInvitation { get; set; }
        public DateTime JoinedDate { get; set; } = DateTime.Now;

    }
}
