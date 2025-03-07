using DevDash.DTO.Account;
using DevDash.DTO.User;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using DevDash.model;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
           
            if (registerDTO == null)
                return BadRequest(new { message = "Input data is null" });

            if (!ModelState.IsValid)
            {
                var errors = ModelState
               .Where(ms => ms.Value.Errors.Count > 0)
               .ToDictionary(
                kvp => kvp.Key,
                 kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = errors
                });
            }
            var existingUser = await _userRepository.GetAsync(u => u.Email==registerDTO.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Account is already registered" });

            var user = await _userRepository.Register(registerDTO);

            if (user == null)
                return StatusCode(500, new { message = "User registration failed due to a server error" });

            return Ok(new { message = "Registration successful", user });
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
              .Where(ms => ms.Value.Errors.Count > 0)
              .ToDictionary(
               kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
               );

                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = errors
                });
            }

            var tokenDTO = await _userRepository.Login(loginDTO);
            if (string.IsNullOrEmpty(tokenDTO.AccessToken))
                return Unauthorized(new { message = "Invalid credentials" });
            return Ok(tokenDTO);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] TokenDTO tokenDTO)
        {
            await _userRepository.RevokeRefreshToken(tokenDTO);
            return Ok(new { message = "Logged out successfully" });
        }

        //[HttpPost("RefreshToken")]
        //public async Task<IActionResult> RefreshToken([FromBody] TokenDTO tokenDTO)
        //{
        //    var newToken = await _userRepository.RefreshAccessToken(tokenDTO);
        //    if (string.IsNullOrEmpty(newToken.AccessToken))
        //        return Unauthorized(new { message = "Invalid refresh token" });

        //    return Ok(newToken);
        //}

        [HttpGet("Profile")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _userRepository.GetUserProfile(int.Parse(userId));
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token" });

            var result = await _userRepository.ChangePassword(int.Parse(userId), changePasswordDTO);
            if (!result)
                return BadRequest(new { message = "Password change failed" });

            return Ok(new { message = "Password changed successfully" });
        }

        //[HttpPost("ResetPassword")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        //{
        //    var result = await _userRepository.ResetPassword(resetPasswordDTO);
        //    if (!result)
        //        return BadRequest(new { message = "Password reset failed" });

        //    return Ok(new { message = "Password reset successfully" });
        //}

        [HttpPost("UpdateProfile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileDTO updateProfileDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token" });

            var updatedUser = await _userRepository.UpdateUserProfile(int.Parse(userId), updateProfileDTO);
            if (updatedUser == null)
                return BadRequest(new { message = "Profile update failed" });

            return Ok(updatedUser);
        }

        [HttpDelete("RemoveAccount")]
        [Authorize]
        public async Task<IActionResult> RemoveAccount()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token" });

            var success = await _userRepository.RemoveAccount(int.Parse(userId));
            if (!success)
                return BadRequest(new { message = "Account removal failed" });

            return Ok(new { message = "Account removed successfully" });
        }
    }
}