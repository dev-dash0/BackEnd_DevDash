using AutoMapper;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
        private readonly IMapper _mapper;
        private APIResponse _response;

        public IssueController(IIssueRepository dbissue, IProjectRepository dbProject, ISprintRepository dbSprint,
            ITenantRepository dbTenant, IMapper mapper)
        {
            _dbissue = dbissue;
            _dbTenant = dbTenant;
            _dbProject = dbProject;
            _dbSprint = dbSprint;
            _mapper = mapper;
            this._response = new APIResponse();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetIssues([FromQuery] int? SprintId
            , [FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                var sprint = await _dbSprint.GetAsync(s => s.Id == SprintId);
                IEnumerable<Issue> Issues = new List<Issue>();
                if (SprintId != null)
                {
                    Issues = await _dbissue.GetAllAsync(
                        filter: i => i.SprintId == SprintId && i.IsBacklog == false && (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                        includeProperties: "Sprint",
                        pageSize: pageSize,
                        pageNumber: pageNumber
                    );
                }
                else
                {
                    Issues = await _dbissue.GetAllAsync(
                        filter: i => i.IsBacklog && i.ProjectId == sprint.ProjectId && (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                        includeProperties: "Sprint",
                        pageSize: pageSize,
                        pageNumber: pageNumber
                    );
                }
                _response.Result = _mapper.Map<List<IssueDTO>>(Issues);
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

        [HttpGet("{id:int}", Name = "GetIssue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetIssue(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                var model = await _dbissue.GetAsync(u => u.Id == id);
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<IssueDTO>(model);
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
        public async Task<ActionResult<APIResponse>> CreateIssue([FromBody] IssueCreataDTO createDTO)
        {
            try
            {

                if (createDTO == null)
                {
                    return BadRequest("IssueCreateDTO cannot be null");
                }


                Project project = await _dbProject.GetAsync(P => P.Id == createDTO.ProjectId);
                if (project == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Project ID is Invalid!");
                    return BadRequest(ModelState);
                }

                if (project.TenantId != createDTO.TenantId)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant ID does not match the project's Tenant ID!");
                    return BadRequest(ModelState);
                }


                Tenant tenant = await _dbTenant.GetAsync(T => T.Id == project.TenantId);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant ID is Invalid!");
                    return BadRequest(ModelState);
                }
                if (createDTO.SprintId != null)
                {
                    Sprint sprint = await _dbSprint.GetAsync(S => S.Id == createDTO.SprintId && S.ProjectId == project.Id);
                    if (sprint == null)
                    {
                        ModelState.AddModelError("ErrorMessages", "Sprint ID is Invalid!");
                        return BadRequest(ModelState);
                    }
                }
                if (createDTO.SprintId != null && createDTO.IsBacklog == true)
                {
                    ModelState.AddModelError("ErrorMessages", "IsBacklog should be false when sprintId not Null");
                    return BadRequest(ModelState);
                }
                if (createDTO.SprintId == null && createDTO.IsBacklog == false)
                {
                    ModelState.AddModelError("ErrorMessages", "IsBacklog should be True when sprintId equal Null");
                    return BadRequest(ModelState);
                }

                Issue issue = _mapper.Map<Issue>(createDTO);
                issue.CreationDate = DateTime.UtcNow;


                await _dbissue.CreateAsync(issue);


                _response.Result = _mapper.Map<IssueDTO>(issue);
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetIssue", new { id = issue.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpDelete("{id:int}", Name = "DeleteIssue")]
        public async Task<ActionResult<APIResponse>> DeleteIssue(int id)
        {
            try
            {
                if (id == 0)
                {
                    return BadRequest();
                }
                var issue = await _dbissue.GetAsync(u => u.Id == id);
                if (issue == null)
                {
                    return NotFound();
                }
                await _dbissue.RemoveAsync(issue);
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



        [HttpPut("{id:int}", Name = "UpdateIssue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateIssue(int id, [FromBody] IssueUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || id != updateDTO.Id)
                {
                    return BadRequest();
                }
                var issue = await _dbissue.GetAsync(u => u.Id == updateDTO.Id);
                if (issue == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Issue ID is Invalid!");
                    return BadRequest(ModelState);
                }
                if (updateDTO.SprintId != null && updateDTO.IsBacklog == true)
                {
                    ModelState.AddModelError("ErrorMessages", "IsBacklog should be false when sprintId not Null");
                    return BadRequest(ModelState);
                }
                if (updateDTO.SprintId == null && updateDTO.IsBacklog == false)
                {
                    ModelState.AddModelError("ErrorMessages", "IsBacklog should be True when sprintId equal Null");
                    return BadRequest(ModelState);
                }
                _mapper.Map(updateDTO, issue);

                await _dbissue.UpdateAsync(issue);
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
        [HttpPatch("{id:int}", Name = "PinIssue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PinIssue(int id, JsonPatchDocument<IssueUpdateDTO> patchDTO)
        {
            if (patchDTO == null || id == 0)
            {
                return BadRequest();
            }

            var issue = await _dbissue.GetAsync(u => u.Id == id, tracked: false);

            if (issue == null)
            {
                return NotFound();
            }

            IssueUpdateDTO issueDTO = _mapper.Map<IssueUpdateDTO>(issue);

            patchDTO.ApplyTo(issueDTO, ModelState);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _mapper.Map(issueDTO, issue);

            await _dbissue.UpdateAsync(issue);

            return NoContent();
        }
    }
}
