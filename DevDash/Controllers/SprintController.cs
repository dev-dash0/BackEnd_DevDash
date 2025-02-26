using AutoMapper;
using DevDash.DTO.Sprint;
using DevDash.Migrations;
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
    public class SprintController : Controller
    {
        private readonly ISprintRepository _dbSprint;
        private readonly IProjectRepository _dbProject;
        private readonly ITenantRepository _dbTenant;
        private readonly IUserProjectRepository _dbUserProject;
        private readonly IUserRepository _dbUser;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public SprintController(ISprintRepository sprintRepo, IMapper mapper, IProjectRepository dbProject,
            ITenantRepository dbTenant, IUserProjectRepository dbUserProject,IUserRepository dbUser)
        {
            _dbSprint = sprintRepo;
            _dbProject = dbProject;
            _dbTenant = dbTenant;
            _dbUser = dbUser;
            _mapper = mapper;
            this._response = new APIResponse();
            _dbUserProject = dbUserProject;
        }

        [HttpGet( Name = "GetSprints")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetSprints([FromQuery] int projectid, [FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                var userId =  User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _dbUser.GetAsync(u=>u.Id == int.Parse(userId));  
                if(user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var userProject = await _dbUserProject.GetAsync(up=> up.ProjectId== projectid && up.UserId == user.Id);
                if (userProject == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User not found in project!");
                    return Unauthorized(ModelState);
                }

                IEnumerable<Sprint> Sprints = await _dbSprint.GetAllAsync(
                   filter: s => s.ProjectId == projectid && (string.IsNullOrEmpty(search) || s.Status.ToLower().Contains(search.ToLower())|| s.Title.ToLower().Contains(search.ToLower())),
                   includeProperties: "Project",
                   pageSize: pageSize,
                   pageNumber: pageNumber
               );
                _response.Result = _mapper.Map<List<SprintDTO>>(Sprints);
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpGet("{sprintId:int}", Name = "GetSprint")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetSprint([FromRoute] int sprintId)
        {
            try
            {
                if (sprintId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _dbUser.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var sprint = await _dbSprint.GetAsync(u => u.Id == sprintId);
                if (sprint == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Sprint ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.ProjectId == sprint.ProjectId && up.UserId == user.Id);
                if (userProject == null)
                {
                    return Unauthorized();
                }

                _response.Result = _mapper.Map<SprintDTO>(sprint);
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateSprint([FromQuery] int projectId,[FromBody] SprintCreateDTO createDTO)
        {
            try
            {
                if (createDTO == null)
                {
                    return BadRequest("SprintCreateDTO cannot be null");
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

                Project project = await _dbProject.GetAsync(P => P.Id == projectId);
                if (project == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Project ID is Invalid!");
                    return BadRequest(ModelState);
                }

                Tenant tenant = await _dbTenant.GetAsync(T => T.Id == project.TenantId);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.ProjectId == project.Id && up.UserId == user.Id);
                if (userProject == null ||userProject.Role == "Developer")
                {
                    ModelState.AddModelError("ErrorMessages", "You Don't have Access to Creata Sprint on this Project");
                    return Unauthorized(ModelState);
                }
                SprintDTO sprintDTO = _mapper.Map<SprintDTO>(createDTO);
                sprintDTO.TenantId = tenant.Id;
                sprintDTO.ProjectId = project.Id;
                sprintDTO.CreatedById = user.Id;
                    
                Sprint sprint = _mapper.Map<Sprint>(sprintDTO);
                await _dbSprint.CreateAsync(sprint);

                _response.Result = new
                {
                    id=sprint.Id,
                    sprint= sprintDTO
                };
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetSprint", new { sprintId = sprint.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpDelete("{sprintId:int}", Name = "DeleteSprint")]
        public async Task<ActionResult<APIResponse>> DeleteSprint([FromRoute] int sprintId)
        {
            try
            {
                if (sprintId == 0 || sprintId == null)
                {
                    return BadRequest();
                }

                var sprint = await _dbSprint.GetAsync(u => u.Id == sprintId);
                if (sprint == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Sprint ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }
                
                var userProject = await _dbUserProject.GetAsync(up=> up.ProjectId == sprint.ProjectId && up.UserId == int.Parse(userId));
                if (userProject == null || userProject.Role == "Developer")
                {
                    ModelState.AddModelError("ErrorMessages", "You Don't have Access to Delete Sprint on this Project");
                    return Unauthorized(ModelState);
                }

                await _dbSprint.RemoveAsync(sprint); 
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }



        [HttpPut("{sprintId:int}", Name = "UpdateSprint")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateSprint([FromRoute] int sprintId, [FromBody] SprintUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || sprintId != updateDTO.Id)
                {
                    return BadRequest();
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var sprint = await _dbSprint.GetAsync(u => u.Id == updateDTO.Id);
                if (sprint == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Sprint ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.ProjectId == sprint.ProjectId && up.UserId == int.Parse(userId));
                if (userProject == null || userProject.Role == "Developer")
                {
                    ModelState.AddModelError("ErrorMessages", "You Don't have Access to Update Sprint on this Project");
                    return Unauthorized(ModelState);
                }


                _mapper.Map(updateDTO, sprint);

                await _dbSprint.UpdateAsync(sprint);
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




    }
}
