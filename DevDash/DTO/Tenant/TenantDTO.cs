using DevDash.model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevDash.DTO.User;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;



namespace DevDash.DTO.Tenant
{
    public class TenantDTO
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public  string Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(255)]
        public string? TenantUrl { get; set; }

        public string? TenantCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);

        public string? Keywords { get; set; }
        public string? Image { get; set; }

        [Required]
        public int OwnerID { get; set; }
        [ValidateNever]
        public UserDTO Owner { get; set; }
        [ValidateNever]
        public ICollection<UserDTO>? JoinedUsers { get; set; }

  



    }
}
