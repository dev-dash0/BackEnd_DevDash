using DevDash.DTO.Account;
using DevDash.DTO.User;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using DevDash.model;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public AccountController(IUserRepository userRepository, IConfiguration config, AppDbContext db)
        {

            _userRepository = userRepository;
            _config = config;
            _db = db;
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
            var existingUser = await _userRepository.GetAsync(u => u.Email == registerDTO.Email);
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

        [HttpGet("google-login")]

        public IActionResult LoginGoogle()
        {
            var clientId = _config["Authentication:Google:ClientId"];
            var redirectUri = "http://devdash.runasp.net/api/Account/google-callback";

            var authUrl = $"https://accounts.google.com/o/oauth2/auth?" +
                         $"response_type=code&" +
                         $"client_id={clientId}&" +
                         $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                         $"scope=openid%20profile%20email&" +
                         $"access_type=offline"
                         ;  // Forces account selection
            return Redirect(authUrl);  // Immediate redirect;


        }




        [HttpGet("google-callback")]
        public async Task<IActionResult> AuthGoogle([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code is missing.");
            }

            var tokenUrl = "https://accounts.google.com/o/oauth2/token";
            var tokenRequestData = new Dictionary<string, string>
    {
        {"code", code},
        {"client_id", _config["Authentication:Google:ClientId"]},
        {"client_secret", _config["Authentication:Google:ClientSecret"]},
        {"redirect_uri", "http://devdash.runasp.net/api/Account/google-callback"},
        {"grant_type", "authorization_code"}
    };

            using var _httpClient = new HttpClient();
            var tokenResponse = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenRequestData));
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return BadRequest("Failed to exchange authorization code for token.");
            }

            var tokenResponseString = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<dynamic>(tokenResponseString);
            string googleAccessToken = tokenData.access_token;

            var userInfoResponse = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v1/userinfo?access_token=" + googleAccessToken);
            if (!userInfoResponse.IsSuccessStatusCode)
            {
                return BadRequest("Failed to retrieve user info from Google.");
            }

            var userInfoString = await userInfoResponse.Content.ReadAsStringAsync();
            var userInfo = JsonConvert.DeserializeObject<dynamic>(userInfoString);
            var email = (string)userInfo.email;

            var user = await _userRepository.GetAsync(u => u.Email == email);
            if (user == null)
            {
                string randomPassword = "Person123#";
            

                var registerDTO = new RegisterDTO
                {
                    FirstName = userInfo.given_name ?? "",
                    LastName = userInfo.family_name ?? "",
                    Username = userInfo.given_name+"."+userInfo.family_name,
                    Email = userInfo.email,
                    Password = randomPassword,
                };
                await _userRepository.Register(registerDTO);
            }
            
            var loginDTO = new LoginWithGoogleDTO
            {
                Email = userInfo.email,
            };
            var tokenDTO = await _userRepository.LoginWithGoogle(loginDTO);
            return Ok(new { UserInfo = userInfo, Token = tokenDTO });
        }

        private string GenerateRandomPassword()
        {
            return $"Rand@{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        private string GenerateRandomPhoneNumber()
        {
            Random random = new Random();
            return "012" + random.Next(10000000, 99999999).ToString(); 
        }









    }
}