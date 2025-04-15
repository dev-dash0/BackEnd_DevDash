using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.User
{
    public class UserCreateDTO
    {
        [Required]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public int TenantId { get; set; }
    }
}
