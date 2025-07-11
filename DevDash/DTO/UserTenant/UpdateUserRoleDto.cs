using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.UserTenant
{
    public class UpdateUserRoleDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(Admin|Project Manager|Developer)$", ErrorMessage = "Invalid role.")]
        public string NewRole { get; set; }
    }
}
