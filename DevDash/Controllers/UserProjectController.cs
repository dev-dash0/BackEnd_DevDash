using AutoMapper;
using DevDash.DTO.UserProject;
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
    public class UserProjectController : ControllerBase
    {
        private readonly IUserProjectRepository _userProjectRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserTenantRepository _userTenantRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IMapper _mapper;
        private readonly APIResponse _response = new();

        public UserProjectController(
            IUserProjectRepository userProjectRepository,
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            IUserTenantRepository userTenantRepository,
            ITenantRepository tenantRepository,
            IMapper mapper)
        {
            _userProjectRepository = userProjectRepository;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _userTenantRepository = userTenantRepository;
            _tenantRepository = tenantRepository;
            _mapper = mapper;
        }

        [HttpPost("join")]
        public async Task<ActionResult<APIResponse>> JoinProject([FromQuery] string projectCode)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _userRepository.GetAsync(u => u.Id == userId);
                var project = await _projectRepository.GetAsync(p => p.ProjectCode == projectCode);

                if (project == null)
                    return BadRequest("Invalid Project Code");

                var tenant = await _tenantRepository.GetAsync(t => t.Id == project.TenantId);
                var userTenant = await _userTenantRepository.GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);
                if (userTenant == null)
                    return BadRequest("User must join the Tenant first");

                var existing = await _userProjectRepository.GetAsync(up => up.UserId == user.Id && up.ProjectId == project.Id);
                if (existing != null)
                    return BadRequest("User already joined this project");

                var entity = new UserProject
                {
                    UserId = user.Id,
                    ProjectId = project.Id,
                    Role = "Developer",
                    JoinedDate = DateTime.UtcNow,
                    AcceptedInvitation = true
                };

                await _userProjectRepository.JoinAsync(entity, userId);
                _response.Result = _mapper.Map<UserProjectDTO>(entity);
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

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUserToProject([FromBody] InviteToProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            int inviterUserId = int.Parse(userIdClaim);
            try
            {
                var result = await _userProjectRepository.InviteByEmailAsync(inviterUserId, dto.Email, dto.ProjectId, dto.Role);

                return Ok(new
                {
                    success = true,
                    message = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Failed to send invitation: {ex.Message}"
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("accept")]
        public async Task<IActionResult> AcceptProjectInvitation([FromQuery] string email, [FromQuery] int projectId)
        {
            if (string.IsNullOrWhiteSpace(email) || projectId <= 0)
                return BadRequest(new { success = false, message = "Invalid invitation parameters." });

            try
            {
                var result = await _userProjectRepository.AcceptInvitationAsync(email, projectId);

                return Ok(new
                {
                    success = true,
                    message = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Failed to accept invitation: {ex.Message}"
                });
            }
        }

        [HttpDelete("{projectId:int}")]
        public async Task<IActionResult> LeaveProject(int projectId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _userRepository.GetAsync(u => u.Id == userId);

            var entity = await _userProjectRepository.GetAsync(up => up.UserId == user.Id && up.ProjectId == projectId);
            if (entity == null) return BadRequest("User not in project");

            await _userProjectRepository.LeaveAsync(entity, userId);
            return NoContent();
        }

        [HttpPatch("update-role")]
        public async Task<IActionResult> UpdateProjectUserRole([FromQuery] int projectId, [FromBody] UpdateUserRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int inviterUserId))
                return BadRequest(new { success = false, message = "Invalid user identity." });

            try
            {
                var result = await _userProjectRepository.UpdateUserRoleAsync(inviterUserId, projectId, dto);
                return Ok(new { success = true, message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
