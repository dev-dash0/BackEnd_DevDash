using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Account
{
    public class RegisterDTO
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string FirstName { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string LastName { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Username { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; } = "Random@123";
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [Phone]
        public string? PhoneNumber { get; set; } = "01285160810";
        public DateOnly? Birthday { get; set; } 

        //    [Phone]
        //    [MaxLength(50)]
        //    //[RegularExpression(@"^[0-9\+]{10,15}$")]
        //    [Required(ErrorMessage = "Phone number is required.")]
        //    public  string PhoneNumber { get; set; } = string.Empty;
        //}
    }
}
