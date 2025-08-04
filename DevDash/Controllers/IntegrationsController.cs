using DevDash.DTO.Integrations.Github;
using DevDash.model;
using DevDash.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DevDash.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class IntegrationsController: ControllerBase
    {
        private readonly IGitHubService _githubService;
        private readonly IJitsiService _jitsiService;
        private readonly UserManager<User> _userManager;
        public IntegrationsController(IGitHubService githubService, IJitsiService jitsiService, UserManager<User> userManager)
        {
            _githubService = githubService;
            _jitsiService = jitsiService;
            _userManager = userManager;
        }

        [HttpPost("github/start")]
        public async Task<IActionResult> StartGitHubIntegration([FromBody] EnableGithubRequestsDTO dto, [FromQuery] string state = "user")
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }
            var userId = user.Id;
            var redirectUrl = _githubService.GetOAuthRedirectUrl("user", dto, userId.ToString());
            return Ok(new { redirectUrl });
        }

        [HttpGet("github/callback")]
        public async Task<IActionResult> HandleOAuthCallback([FromQuery] string code, [FromQuery] string state = "user")
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Missing code");
            try
            {
                await _githubService.HandleOAuthCallbackAsync(code, state);
                return Ok("GitHub repo created successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error during GitHub OAuth callback: {ex.Message}");
            }
        }

        //[HttpPost("github/disable")]
        //public async Task<IActionResult> DisableGitHubIntegration()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null)
        //    {
        //        return Unauthorized("User not found");
        //    }
        //    var userId = user.Id;
        //    try
        //    {
        //        await _githubService.DisableAsync(userId.ToString());
        //        return Ok("GitHub integration disabled successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest($"Error disabling GitHub integration: {ex.Message}");
        //    }
        //}

        [HttpGet("github/repos")]
        public async Task<IActionResult> GetUserRepos()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }
            var userId = user.Id;
            try
            {
                var repos = await _githubService.GetUserReposAsync(userId.ToString());
                return Ok(repos);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error fetching GitHub repositories: {ex.Message}");
            }
        }


        [HttpPost("jitsi/create-meeting")]
        public async Task<IActionResult> CreateJitsiMeeting()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not found");

            var meeting = await _jitsiService.CreateMeetingAsync(user.Email, user.UserName);
            return Ok(meeting);
        }

    }
}
