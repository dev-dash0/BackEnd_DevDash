using AutoMapper;
using DevDash.DTO.Issue;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssueController : ControllerBase
    {
        private readonly IIssueRepository _dbissue;
        private readonly IProjectRepository _dbProject;
        private readonly ISprintRepository _dbSprint;
        private readonly ITenantRepository _dbTenant;
        private readonly IUserProjectRepository _dbUserProject;
        private readonly IIssueAssignUserRepository _dbIssueAssignUser;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public IssueController(IIssueRepository dbissue, IProjectRepository dbProject, ISprintRepository dbSprint,
            ITenantRepository dbTenant, IUserProjectRepository dbUserProjec, IIssueAssignUserRepository dbIssueAssignUser, IMapper mapper)
        {
            _dbissue = dbissue;
            _dbTenant = dbTenant;
            _dbProject = dbProject;
            _dbSprint = dbSprint;
            _dbUserProject = dbUserProjec;
            _dbIssueAssignUser = dbIssueAssignUser;
            _mapper = mapper;
            this._response = new APIResponse();
        }

        [HttpGet("backlog")]
        public async Task<ActionResult<APIResponse>> GetBacklogIssues([FromQuery] int projectId, [FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Invalid token" });

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == int.Parse(userId) && up.ProjectId == projectId);
                if (userProject == null) return Forbid();

                IEnumerable<Issue> Issues = new List<Issue>();

                if (userProject.Role == "Admin" || userProject.Role == "Project Manager")
                {
                    Issues = await _dbissue.GetAllAsync(
                        filter: i => i.ProjectId == projectId && i.SprintId == null && i.IsBacklog &&
                                     (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                        includeProperties: "CreatedBy",
                        pageSize: pageSize,
                        pageNumber: pageNumber
                    );
                }
                else if (userProject.Role == "Developer")
                {
                    Issues = await _dbissue.GetAllAsync(
                        filter: i => i.ProjectId == projectId && i.SprintId == null && i.IsBacklog &&
                                     i.IssueAssignedUsers.Any(iau => iau.UserId == int.Parse(userId)) &&
                                     (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                        includeProperties: "",
                        pageSize: pageSize,
                        pageNumber: pageNumber
                    );
                }
                else
                {
                    return Forbid();
                }

                _response.Result = _mapper.Map<List<IssueDTO>>(Issues);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }
        [HttpGet("sprint")]
        public async Task<ActionResult<APIResponse>> GetSprintIssues([FromQuery] int sprintId, [FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Invalid token" });

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == int.Parse(userId) && up.Project.Sprints.Any(s => s.Id == sprintId));
                if (userProject == null) return Forbid();

                IEnumerable<Issue> Issues = new List<Issue>();

                if (userProject.Role == "Admin" || userProject.Role == "Project Manager")
                {
                    Issues = await _dbissue.GetAllAsync(
                        filter: i => i.SprintId == sprintId &&
                                     (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                        includeProperties: "",
                        pageSize: pageSize,
                        pageNumber: pageNumber
                    );
                }
                else if (userProject.Role == "Developer")
                {
                    Issues = await _dbissue.GetAllAsync(
                        filter: i => i.SprintId == sprintId &&
                                     i.IssueAssignedUsers.Any(iau => iau.UserId == int.Parse(userId)) &&
                                     (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                        includeProperties: "CreatedBy",
                        pageSize: pageSize,
                        pageNumber: pageNumber
                    );
                }
                else
                {
                    return Forbid();
                }

                _response.Result = _mapper.Map<List<IssueDTO>>(Issues);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }


        [HttpGet("{id:int}", Name = "GetIssue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetIssue(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string> { "Invalid Issue ID" };
                    return BadRequest(_response);
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Invalid token" });

                int userIdInt = int.Parse(userId);

                var issue = await _dbissue.GetAsync(
                    i => i.Id == id,
                    includeProperties: "CreatedBy"
                );

                if (issue == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "Issue not found" };
                    return NotFound(_response);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == userIdInt && up.ProjectId == issue.ProjectId);
                if (userProject == null) return Forbid();

                if (userProject.Role != "Admin" && userProject.Role != "Project Manager")
                {
                    bool isAssignedToUser = issue.IssueAssignedUsers.Any(iau => iau.UserId == userIdInt);

                    if (!isAssignedToUser)
                    {
                        return Forbid();
                    }
                }

                _response.Result = _mapper.Map<IssueDTO>(issue);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpPost("backlog")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateBacklogIssue([FromQuery] int projectId, [FromBody] IssueCreataDTO createDTO)
        {
            try
            {
                if (createDTO == null)
                    return BadRequest(new { message = "IssueCreateDTO cannot be null" });

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == int.Parse(userId) && up.ProjectId == projectId);
                if (userProject == null || (userProject.Role != "Admin" && userProject.Role != "Project Manager"))
                    return Unauthorized(new { message = "You do not have permission to create an issue in this project" });

                Project project = await _dbProject.GetAsync(p => p.Id == projectId);
                if (project == null)
                    return BadRequest(new { message = "Project ID is Invalid!" });

                createDTO.IsBacklog = true;
                createDTO.ProjectId = projectId;
                createDTO.CreatedById = int.Parse(userId);
                createDTO.TenantId = project.TenantId;

                Issue issue  = _mapper.Map<Issue>(createDTO);
                await _dbissue.CreateAsync(issue);
               var IssueDTO= _mapper.Map<IssueDTO>(issue);
                _response.Result = new
                {
                    id=issue.Id,
                    issue= IssueDTO
                };
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetIssue", new { id = issue.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpPost("sprint")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateSprintIssue([FromQuery] int sprintId, [FromBody] IssueCreataDTO createDTO)
        {
            try
            {
                if (createDTO == null)
                    return BadRequest(new { message = "IssueCreateDTO cannot be null" });

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                var sprint = await _dbSprint.GetAsync(s => s.Id == sprintId);
                if (sprint == null)
                    return BadRequest(new { message = "Sprint ID is Invalid!" });

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == int.Parse(userId) && up.ProjectId == sprint.ProjectId);
                if (userProject == null || (userProject.Role != "Admin" && userProject.Role != "Project Manager"))
                    return Unauthorized(new { message = "You do not have permission to create an issue in this project" });

                createDTO.IsBacklog = false;
                createDTO.ProjectId = sprint.ProjectId;
                createDTO.CreatedById = int.Parse(userId);
                createDTO.TenantId = sprint.TenantId;
                createDTO.SprintId = sprintId;

                Issue issue = _mapper.Map<Issue>(createDTO);
                await _dbissue.CreateAsync(issue);
                var IssueDTO = _mapper.Map<IssueDTO>(issue);
                _response.Result = new
                {
                    id=issue.Id,
                    issue = IssueDTO
                };
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetIssue", new { id = issue.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }




        [HttpDelete("{id:int}", Name = "DeleteIssue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> DeleteIssue(int id)
        {
            try
            {
                if (id == 0)
                    return BadRequest(new { message = "Invalid issue ID" });

                var issue = await _dbissue.GetAsync(i => i.Id == id);
                if (issue == null)
                    return NotFound(new { message = "Issue not found" });

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == int.Parse(userId) && up.ProjectId == issue.ProjectId);
                if (userProject == null || (userProject.Role != "Admin" && userProject.Role != "Project Manager"))
                    return Unauthorized(new { message = "You do not have permission to delete this issue" });

                await _dbissue.RemoveAsync(issue);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return NoContent();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }



        [HttpPut("{id:int}", Name = "UpdateIssue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> UpdateIssue(int id, [FromBody] IssueUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || id != updateDTO.Id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string> { "Invalid issue ID or data" };
                    return BadRequest(_response);
                }

                var issue = await _dbissue.GetAsync(i => i.Id == updateDTO.Id);
                if (issue == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "Issue not found" };
                    return NotFound(_response);
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string> { "Invalid token" };
                    return Unauthorized(_response);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == int.Parse(userId) && up.ProjectId == issue.ProjectId);
                bool hasPermission = userProject != null && (userProject.Role == "Admin" || userProject.Role == "Project Manager");

                if (!hasPermission)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string> { "You do not have permission to update this issue" };
                    return Unauthorized();
                }

                if (updateDTO.SprintId != 0) updateDTO.IsBacklog = false;
                else
                {
                    updateDTO.IsBacklog = true;
                    updateDTO.SprintId = null;
                }

                _mapper.Map(updateDTO, issue);
                await _dbissue.UpdateAsync(issue);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return NoContent();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }



        //[HttpPatch("{id:int}", Name = "PinIssue")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> PinIssue(int id, JsonPatchDocument<IssueUpdateDTO> patchDTO)
        //{
        //    if (patchDTO == null || id == 0)
        //    {
        //        return BadRequest();
        //    }

        //    var issue = await _dbissue.GetAsync(u => u.Id == id, tracked: false);

        //    if (issue == null)
        //    {
        //        return NotFound();
        //    }

        //    IssueUpdateDTO issueDTO = _mapper.Map<IssueUpdateDTO>(issue);

        //    patchDTO.ApplyTo(issueDTO, ModelState);
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    _mapper.Map(issueDTO, issue);

        //    await _dbissue.UpdateAsync(issue);

        //    return NoContent();
        //}
    }
}
