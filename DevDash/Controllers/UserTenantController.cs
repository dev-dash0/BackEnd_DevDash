using AutoMapper;
using Azure;
using DevDash.DTO.Tenant;
using DevDash.DTO.UserTenant;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserTenantController : ControllerBase
    {
        private IUserTenantRepository _userTenantRepository;
        private ITenantRepository _tenantRepository;
        private IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public UserTenantController(ITenantRepository tenantRepository, IUserRepository userRepository,
             IMapper mapper, IUserTenantRepository userTenantRepository)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            this._response = new APIResponse();
            _userTenantRepository = userTenantRepository;
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

        [HttpPost]
        public async Task<ActionResult<APIResponse>> JoinTenant([FromQuery]string tenantCode)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var tenant = await _tenantRepository.GetAsync(t => t.TenantCode == tenantCode);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant Code is Invalid!");
                    return BadRequest(ModelState);
                }

                var existingUserTenant = await _userTenantRepository
                    .GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);

                if (existingUserTenant != null)
                {
                    ModelState.AddModelError("ErrorMessages", "User is already a member of this Tenant!");
                    return BadRequest(ModelState);
                }

                UserTenantDTO userTenantDTO = new()
                {
                    UserId = user.Id,
                    TenantId = tenant.Id,
                    Role = "Developer",
                    JoinedDate = DateTime.Now,
                };
                UserTenant userTenant = _mapper.Map<UserTenant>(userTenantDTO);
                await _userTenantRepository.JoinAsync(userTenant,int.Parse(userId));

                _response.Result = userTenantDTO;
                _response.StatusCode = HttpStatusCode.Created;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }
            return _response;
        }

        [HttpDelete("{tenantId:int}", Name = "LeaveTenant")]
        public async Task<ActionResult<APIResponse>> LeaveTenant([FromRoute] int tenantId)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var tenant = await _tenantRepository.GetAsync(t => t.Id == tenantId);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant Code is Invalid!");
                    return BadRequest(ModelState);
                }

                var existingUserTenant = await _userTenantRepository
                    .GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);

                if (existingUserTenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User is already out of this Tenant!");
                    return BadRequest(ModelState);
                }

                
                await _userTenantRepository.LeaveAsync(existingUserTenant,int.Parse(userId));


                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }
            return _response;
        }




    }
}
