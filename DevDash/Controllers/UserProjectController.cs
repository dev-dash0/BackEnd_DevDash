using AutoMapper;
using Azure;
using DevDash.DTO;
using DevDash.DTO.Tenant;
using DevDash.DTO.UserProject;
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
    public class UserProjectController : ControllerBase
    {
        private ITenantRepository _tenant;
        private IProjectRepository _project;
        private IUserRepository _user;
        private IUserProjectRepository _userProject;
        private IUserTenantRepository _userTenant;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public UserProjectController(IUserProjectRepository userProject, ITenantRepository tenant,
            IUserRepository user, IMapper mapper, IProjectRepository project, IUserTenantRepository userTenant)
        {
            this._response = new APIResponse();
            _tenant = tenant;
            _project = project;
            _mapper = mapper;
            _user = user;
            _userProject = userProject;
            _userTenant = userTenant;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> JoinProject([FromQuery] string projectCode)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                var user = await _user.GetAsync(u => u.Id == int.Parse(userId));


                var project = await _project.GetAsync(p => p.ProjectCode == projectCode);
                if (project == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant Code is Invalid!");
                    return BadRequest(ModelState);
                }

                var tenant = await _tenant.GetAsync(t => t.Id == project.TenantId);

                //check if this user found on this tenant 

                var userTenant = await _userTenant.GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);
                if (userTenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", $"User Should Join Tenant First before Join Project");
                    return BadRequest(ModelState);
                }


                var existingUserProject = await _userProject
                    .GetAsync(ut => ut.UserId == user.Id && ut.ProjectId == project.Id);

                if (existingUserProject != null)
                {
                    ModelState.AddModelError("ErrorMessages", "User is already a member in this Project!");
                    return BadRequest(ModelState);
                }

                UserProjectDTO userProjectDTO = new()
                {
                    UserId = user.Id,
                    ProjectId = project.Id,
                    Role = "Developer",
                    JoinedDate = DateTime.Now,
                };

                UserProject userProject = _mapper.Map<UserProject>(userProjectDTO);
                await _userProject.JoinAsync(userProject,int.Parse(userId));

                _response.Result = userProjectDTO;
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

        [HttpDelete("{projectId:int}", Name = "LeaveProject")]
        public async Task<ActionResult<APIResponse>> LeaveProject([FromRoute] int projectId )
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var user = await _user.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var project = await _project.GetAsync(p => p.Id == projectId);
                if (project == null)
                {
                    ModelState.AddModelError("ErrorMessages", "project ID  is Invalid!");
                    return BadRequest(ModelState);
                }

                var existingUserProject = await _userProject
                    .GetAsync(ut => ut.UserId == user.Id && ut.ProjectId == project.Id);

                if (existingUserProject == null)
                {
                    ModelState.AddModelError("ErrorMessages", $"User already not found in this Project!");
                    return BadRequest(ModelState);
                }
                
                await _userProject.LeaveAsync(existingUserProject,int.Parse(userId));
                _response.StatusCode = HttpStatusCode.NoContent;
                return _response;
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
