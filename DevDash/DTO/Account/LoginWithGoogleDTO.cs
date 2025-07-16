using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Account
{
    public class LoginWithGoogleDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
