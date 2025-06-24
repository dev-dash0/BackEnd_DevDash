using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class Tenant
    {
       
        public int Id { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public required string Name { get; set; }
        public string? Description { get; set; }
        public  string TenantCode { get; set; } 

        [MaxLength(255)]
        public string? TenantUrl { get; set; }
        [MaxLength(255)]
        public string? Keywords { get; set; }
        public string? Image { get; set; }
        //Foreign Keys

        [ForeignKey(nameof(User.Id))]
        [Required]
        public int OwnerID { get; set; }

        // Navigation Properties
        public  User Owner { get; set; }
        public ICollection<User>? JoinedUsers { get; set; }
        public ICollection<UserTenant>? UserTenants { get; set; }

        public ICollection<Project>? Projects { get; set; }
    }
}
