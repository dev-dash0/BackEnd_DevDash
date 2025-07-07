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
        private readonly APIResponse _response = new();

        public UserTenantController(ITenantRepository tenantRepository, IUserRepository userRepository,
            IMapper mapper, IUserTenantRepository userTenantRepository)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _userTenantRepository = userTenantRepository;
        }

        [HttpPost("join")]
        public async Task<ActionResult<APIResponse>> JoinTenant([FromBody] JoinTenantDTO dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User not found.");
                    return Unauthorized(_response);
                }

                var tenant = await _tenantRepository.GetAsync(t => t.TenantCode == dto.TenantCode);
                if (tenant == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid tenant code.");
                    return BadRequest(_response);
                }

                var alreadyJoined = await _userTenantRepository.GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);
                if (alreadyJoined != null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User already joined this tenant.");
                    return BadRequest(_response);
                }

                var userTenant = new UserTenant
                {
                    UserId = user.Id,
                    TenantId = tenant.Id,
                    Role = "Developer",
                    JoinedDate = DateTime.Now,
                    User = user,
                    Tenant = tenant
                };

                await _userTenantRepository.JoinAsync(userTenant, user.Id);

                _response.Result = _mapper.Map<UserTenantDTO>(userTenant);
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _response);
            }
        }

        [HttpPost("invite")]
        public async Task<ActionResult<APIResponse>> InviteUserToTenant([FromBody] InviteToTenantDto dto)
        {
            try
            {
                await _userTenantRepository.InviteByEmailAsync(dto.Email, dto.TenantId, dto.Role);

                _response.Result = $"Invitation sent to {dto.Email}";
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        [HttpPost("accept")]
        public async Task<ActionResult<APIResponse>> AcceptTenantInvitation()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _userRepository.GetAsync(u => u.Id == userId);
                if (user == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User not found.");
                    return Unauthorized(_response);
                }

                await _userTenantRepository.AcceptInvitationAsync(user.Email, userId);

                _response.Result = "Invitation accepted successfully.";
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        [HttpDelete("{tenantId:int}")]
        public async Task<ActionResult<APIResponse>> LeaveTenant(int tenantId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _userRepository.GetAsync(u => u.Id == userId);
                if (user == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User not found.");
                    return Unauthorized(_response);
                }

                var userTenant = await _userTenantRepository.GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenantId);
                if (userTenant == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not part of this tenant.");
                    return BadRequest(_response);
                }

                await _userTenantRepository.LeaveAsync(userTenant, user.Id);

                _response.Result = "Left the tenant successfully.";
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _response);
            }
        }
    }
}
