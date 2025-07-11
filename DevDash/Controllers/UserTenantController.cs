using AutoMapper;
using DevDash.DTO.UserTenant;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserTenantController : ControllerBase
    {
        private readonly IUserTenantRepository _userTenantRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly APIResponse _response;

        public UserTenantController(
            ITenantRepository tenantRepository,
            IUserRepository userRepository,
            IMapper mapper,
            IUserTenantRepository userTenantRepository)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _userTenantRepository = userTenantRepository;
            _response = new APIResponse();
        }

        [HttpPost("join")]
        public async Task<ActionResult<APIResponse>> JoinTenant([FromQuery] string tenantCode)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var user = await _userRepository.GetAsync(u => u.Id == userId);
                var tenant = await _tenantRepository.GetAsync(t => t.TenantCode == tenantCode);

                if (user == null || tenant == null)
                    return BadRequest("Invalid user or tenant.");

                var existing = await _userTenantRepository.GetAsync(ut => ut.UserId == userId && ut.TenantId == tenant.Id);
                if (existing != null)
                    return BadRequest("User already joined this tenant.");

                var userTenant = new UserTenant
                {
                    UserId = user.Id,
                    TenantId = tenant.Id,
                    Role = "Developer",
                    AcceptedInvitation = true,
                    JoinedDate = DateTime.UtcNow
                };

                await _userTenantRepository.JoinAsync(userTenant, user.Id);

                _response.Result = _mapper.Map<UserTenantDTO>(userTenant);
                _response.StatusCode = HttpStatusCode.Created;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return _response;
            }
        }

        [HttpDelete("{tenantId:int}")]
        public async Task<ActionResult<APIResponse>> LeaveTenant(int tenantId)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userTenant = await _userTenantRepository.GetAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);

                if (userTenant == null)
                    return BadRequest("User not in this tenant.");

                await _userTenantRepository.LeaveAsync(userTenant, userId);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return _response;
            }
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUserToTenant([FromBody] InviteToTenantDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            int inviterUserId = int.Parse(userIdClaim);
            try
            {
                var result = await _userTenantRepository.InviteByEmailAsync(inviterUserId, dto.Email, dto.TenantId, dto.Role);

                return Ok(new
                {
                    success = true,
                    message = result
                });
            }
            catch (Exception ex)
            {
                // Optional: log exception here
                return BadRequest(new
                {
                    success = false,
                    message = $"Failed to send invitation: {ex.Message}"
                });
            }
        }


        [AllowAnonymous]
        [HttpGet("accept")]
        public async Task<IActionResult> AcceptTenantInvitation([FromQuery] string email, [FromQuery] int tenantId)
        {
            if (string.IsNullOrWhiteSpace(email) || tenantId <= 0)
                return BadRequest(new { success = false, message = "Invalid invitation parameters." });

            try
            {
                var result = await _userTenantRepository.AcceptInvitationAsync(email, tenantId);

                return Ok(new
                {
                    success = true,
                    message = result
                });
            }
            catch (Exception ex)
            {
                // Optional: log exception here
                return BadRequest(new
                {
                    success = false,
                    message = $"Failed to accept invitation: {ex.Message}"
                });
            }
        }

        [HttpPatch("update-role")]
        public async Task<IActionResult> UpdateTenantUserRole([FromQuery] int tenantId, [FromBody] UpdateUserRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int inviterUserId))
                return BadRequest(new { success = false, message = "Invalid user identity." });

            try
            {
                var result = await _userTenantRepository.UpdateUserRoleAsync(inviterUserId, tenantId, dto);
                return Ok(new { success = true, message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

    }
}
