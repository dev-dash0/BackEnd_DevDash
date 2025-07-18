using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Account
{
    public class ForgotPasswordEmailDTO
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }
    }

    public class PasswordTokenDTO
    {
        public string Token { get; set; }
    }

    public class NewPasswordDTO
    {
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string NewPassword { get; set; }
    }
    //public class ResetPasswordDTO
    //{
    //    public string Email { get; set; }
    //    public string Token { get; set; }
    //    public string NewPassword { get; set; }
    //}
}
