using AutoMapper;
using DevDash.DTO.UserProject;
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
        private readonly ITenantRepository _tenantRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserTenantRepository _userTenantRepository;
        private readonly IMapper _mapper;
        private readonly APIResponse _response = new();

        public UserProjectController(
            IUserProjectRepository userProjectRepository,
            ITenantRepository tenantRepository,
            IUserRepository userRepository,
            IMapper mapper,
            IProjectRepository projectRepository,
            IUserTenantRepository userTenantRepository)
        {
            _userProjectRepository = userProjectRepository;
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _projectRepository = projectRepository;
            _userTenantRepository = userTenantRepository;
        }

        [HttpPost("join")]
        public async Task<ActionResult<APIResponse>> JoinProject([FromBody] JoinProjectDTO dto)
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

                var project = await _projectRepository.GetAsync(p => p.ProjectCode == dto.ProjectCode);
                if (project == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid project code.");
                    return BadRequest(_response);
                }

                var tenant = await _tenantRepository.GetAsync(t => t.Id == project.TenantId);
                if (tenant == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Tenant associated with this project does not exist.");
                    return BadRequest(_response);
                }

                var isUserInTenant = await _userTenantRepository.GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);
                if (isUserInTenant == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You must join the tenant before joining the project.");
                    return BadRequest(_response);
                }

                var isUserInProject = await _userProjectRepository.GetAsync(up => up.UserId == user.Id && up.ProjectId == project.Id);
                if (isUserInProject != null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are already a member of this project.");
                    return BadRequest(_response);
                }

                var userProject = new UserProject
                {
                    UserId = user.Id,
                    ProjectId = project.Id,
                    Role = "Developer",
                    JoinedDate = DateTime.Now,
                    User = user,
                    Project = project
                };

                await _userProjectRepository.JoinAsync(userProject, user.Id);
                _response.Result = _mapper.Map<UserProjectDTO>(userProject);
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
        public async Task<ActionResult<APIResponse>> InviteUserToProject([FromBody] InviteToProjectDto dto)
        {
            try
            {
                await _userProjectRepository.InviteByEmailAsync(dto.Email, dto.ProjectId, dto.Role);

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
        public async Task<ActionResult<APIResponse>> AcceptProjectInvitation()
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

                await _userProjectRepository.AcceptInvitationAsync(user.Email, userId);

                _response.Result = "Project invitation accepted successfully.";
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

        [HttpDelete("{projectId:int}")]
        public async Task<ActionResult<APIResponse>> LeaveProject(int projectId)
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

                var userProject = await _userProjectRepository.GetAsync(up => up.UserId == user.Id && up.ProjectId == projectId);
                if (userProject == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not a member of this project.");
                    return BadRequest(_response);
                }

                await _userProjectRepository.LeaveAsync(userProject, user.Id);
                _response.Result = "Left the project successfully.";
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
