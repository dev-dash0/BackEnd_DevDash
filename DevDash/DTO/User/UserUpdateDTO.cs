using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.User
{
    public class UserUpdateDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }
}
