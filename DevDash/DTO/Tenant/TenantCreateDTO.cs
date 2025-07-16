using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Tenant
{
    public class TenantCreateDTO
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public  string Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(255)]
        public string? TenantUrl { get; set; }

        public string? Keywords { get; set; }
        public string? Image { get; set; }

        //[Required]
        //public int OwnerID { get; set; }
    }
}
