using AutoMapper;
using Azure;
using DevDash.DTO.Project;
using DevDash.DTO.Tenant;
using DevDash.DTO.User;
using DevDash.DTO.UserProject;
using DevDash.DTO.UserTenant;
using DevDash.model;
using DevDash.Repository;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectRepository _dbProject;
        private readonly ITenantRepository _dbTenant;
        private readonly IUserRepository _dbUser;
        private readonly IUserProjectRepository _dbUserProject;
        private readonly IUserTenantRepository _dbUserTenant;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public ProjectController(IProjectRepository dbProject, ITenantRepository dbTenant,
            IUserRepository dbUser, IMapper mapper, IUserTenantRepository dbUserTenant, IUserProjectRepository dbUserProject)
        {
            _dbUserProject = dbUserProject;
            _dbUserTenant = dbUserTenant;
            _dbTenant = dbTenant;
            _dbProject = dbProject;
            _dbUser = dbUser;
            _mapper = mapper;
            this._response = new APIResponse();
        }
        [HttpGet(Name = "GetProjects")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetProjects([FromQuery] int tenantId, [FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                if (tenantId <= 0)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string> { "Invalid Tenant ID" };
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string> { "Invalid token" };
                    return Unauthorized(_response);
                }

                var projects = await _dbProject.GetAllAsync(
                    filter: p =>
                         p.TenantId == tenantId &&
                         p.UserProjects.Any(up =>
                             up.UserId == userId &&
                             up.AcceptedInvitation),
                     includeProperties: "Creator,UserProjects,Tenant,Tenant.JoinedUsers",
                     pageSize: pageSize,
                     pageNumber: pageNumber
                 );

                var projectDtos = _mapper.Map<List<ProjectDTO>>(projects);
                foreach (var projectDto in projectDtos)
                {
                    var userTenant = await _dbUserTenant.GetAsync(up => up.UserId == userId && up.TenantId == projectDto.TenantId);
                    if (userTenant != null)
                    {
                        projectDto.Tenant.Role = userTenant.Role;
                        


                    }
                }

                foreach (var item in projectDtos)
                {
                    var userProject = await _dbUserProject.GetAsync(up => up.UserId == userId && up.ProjectId == item.Id);
                    if (userProject != null)
                    {
                        item.Role = userProject.Role;
                    }
                }
                    _response.Result = projectDtos;
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        


        [HttpGet("{projectId:int}", Name = "GetProject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetProject([FromRoute] int projectId)
        {
            try
            {
                if (projectId <= 0)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string> { "Invalid Project ID" };
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string> { "Invalid token" };
                    return Unauthorized(_response);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == userId && up.ProjectId == projectId && up.AcceptedInvitation == true);
                if (userProject == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string> { "You do not have permission to view this project." };
                    return Unauthorized(_response);
                }
                
                var project = await _dbProject.GetAsync(
                    filter: p => p.Id == projectId,
                    includeProperties: "Creator,UserProjects,Tenant,Tenant.JoinedUsers,Sprints"
                );

                if (project == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "Project not found." };
                    return NotFound(_response);
                }
                var usertenant = await _dbUserTenant.GetAsync(ut => ut.UserId == userId && ut.TenantId == userProject.Project.TenantId);
                var projectDto = _mapper.Map<ProjectDTO>(project);
                projectDto.Tenant.Role = usertenant?.Role ;
                projectDto.Role = userProject?.Role;
                _response.Result = projectDto;
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateProject([FromQuery] int tenantId,[FromBody] ProjectCreateDTO createDTO)
        {
            try
            {
                if (createDTO == null)
                {
                    return BadRequest(createDTO);
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var user = await _dbUser.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var tenant = await _dbTenant.GetAsync(u => u.Id == tenantId);
                if (tenant == null) 
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant is invalid");
                    return BadRequest(ModelState);
                }

                var userTenant = await _dbUserTenant.GetAsync(ut=>ut.UserId == user.Id 
                && ut.TenantId == tenant.Id);
                if (userTenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User Not Found On this Tenant");
                    return BadRequest(ModelState);
                }
                if(userTenant.Role !="Admin")
                {
                    return Unauthorized();
                }

                ProjectDTO projectDTO = _mapper.Map<ProjectDTO>(createDTO);
                projectDTO.CreatorId = user.Id;
                projectDTO.TenantId = tenantId;

                Project project = _mapper.Map<Project>(projectDTO);
                await _dbProject.CreateAsync(project);
                
                UserProjectDTO userProjectDTO = new()
                {
                    UserId = user.Id,
                    ProjectId = project.Id,
                    Role = "Admin",
                    JoinedDate = DateTime.Now,
                    AcceptedInvitation = true
                };

                UserProject userProject = _mapper.Map<UserProject>(userProjectDTO);
                await _dbUserProject.JoinAsync(userProject,int.Parse(userId));
                _response.Result = new
                {
                    id=project.Id,
                    project= projectDTO
                } ;
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute("GetProject", new { projectId = project.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpDelete("{projectId:int}", Name = "DeleteProject")]
        public async Task<ActionResult<APIResponse>> DeleteProject([FromRoute]int projectId)
        {
            try
            {
                if (projectId == 0 || projectId == null)
                {
                    return BadRequest();
                }
                var project = await _dbProject.GetAsync(u => u.Id == projectId);

                if (project == null)
                {
                    return NotFound();
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userTenant = await _dbUserTenant.GetAsync(
                    ut => ut.TenantId == project.TenantId && ut.UserId == int.Parse(userId));

                if(userTenant == null || userTenant.Role != "Admin")
                {
                    return Unauthorized();
                }
                
                await _dbProject.RemoveAsync(project);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }



        [HttpPut("{projectId:int}", Name = "UpdateProject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateProject([FromRoute] int projectId, [FromBody] ProjectUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null)
                {
                    return BadRequest();
                }

                var project = await _dbProject.GetAsync(u => u.Id == projectId);
                if (project == null)
                {
                    return NotFound();
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userTenant = await _dbUserTenant.GetAsync(
                    ut => ut.TenantId == project.TenantId && ut.UserId == int.Parse(userId));

                if (userTenant == null || userTenant.Role != "Admin")
                {
                    return Unauthorized();
                }


                _mapper.Map(updateDTO, project);

                await _dbProject.UpdateAsync(project);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpPatch("{projectId:int}", Name = "PinProject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PinProject([FromRoute]int projectId, JsonPatchDocument<ProjectUpdateDTO> patchDTO)
        {
            if (patchDTO == null || projectId == 0)
            {
                return BadRequest();
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token" });

            var userProject = await _dbUserProject.GetAsync(up => up.UserId == int.Parse(userId)
            && up.ProjectId == projectId);

            if(userProject == null)
            {
                return Unauthorized();
            }

            var project = await _dbProject.GetAsync(u => u.Id == projectId, tracked: false);
            if (project == null)
            {
                return NotFound();
            }


            ProjectUpdateDTO projectDTO = _mapper.Map<ProjectUpdateDTO>(project);

            patchDTO.ApplyTo(projectDTO, ModelState);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
   

            _mapper.Map(projectDTO, project);

            await _dbProject.UpdateAsync(project);

            return NoContent();
        }




    }
}
