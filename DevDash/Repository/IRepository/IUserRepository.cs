using DevDash.DTO.Account;
using DevDash.DTO.User;
using DevDash.model;
using DevDash.model;
using System.Threading.Tasks;

namespace DevDash.Repository.IRepository
{
    public interface IUserRepository : IRepository<User>
    {
        bool IsUniqueEmail(string email);
        Task<TokenDTO> Login(LoginDTO loginDTO);
        Task<User?> Register(RegisterDTO registerDTO);
        Task<StepResponseDTO> SendEmail(string email, string subject, string content, string? heading = null);
        Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO);
        Task RevokeRefreshToken(TokenDTO tokenDTO);
        Task<User> UpdateAsync(User user);
        Task RemoveAsync(User user);
        Task<UserDTO> GetUserProfile(int userId);
        Task<TokenDTO> LoginWithGoogle(LoginWithGoogleDTO loginDTO);
        Task<bool> ChangePassword(int userId, ChangePasswordDTO changePasswordDTO);
        Task<StepResponseDTO> SendPasswordResetToken(string email);
        Task<StepResponseDTO> VerifyToken(PasswordTokenDTO passwordTokenDTO);
        Task<StepResponseDTO> ResetPassword(NewPasswordDTO newPasswordDTO);
        Task<UserDTO> UpdateUserProfile(int userId, UpdateProfileDTO updateProfileDTO);

        Task<bool> RemoveAccount(int userId);
    }
}
