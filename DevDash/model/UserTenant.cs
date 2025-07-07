using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DevDash.model
{
    public class UserTenant
    {
        [Key]
        public int UserId { get; set; }

        [Key]
        public int TenantId { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Admin|Developer|Project Manager)$", ErrorMessage = "Invalid role")]
        public required string Role { get; set; }

        public bool AcceptedInvitation { get; set; } = false;

        [DataType(DataType.Date)]
        public DateTime JoinedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public  User User { get; set; }
        public  Tenant Tenant { get; set; }
    }

}
