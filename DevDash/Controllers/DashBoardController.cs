using AutoMapper;
using DevDash.DTO.Account;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.DTO.Sprint;
using DevDash.DTO.Tenant;
using DevDash.Migrations;
using DevDash.model;
using DevDash.Repository;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashBoardController : ControllerBase
    {
        private readonly IDashBoardRepository _DashBoardRepository;
        private readonly ITenantRepository _TenantRepository;
        private readonly IProjectRepository _ProjectRepository;
        private readonly IUserProjectRepository _UserProjectRepository;
        private readonly IUserRepository _UserRepository;
        private readonly ISprintRepository _SprintRepository;
        private readonly IPinnedItemRepository _PinnedItemRepository;
        private readonly IIssueRepository _IssueRepository;
        private readonly AppDbContext _dbContext;
        private readonly IUserTenantRepository _UserTenantRepository;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public DashBoardController(IDashBoardRepository DashBoardRepository, ITenantRepository tenantRepository
            , IUserTenantRepository UserTenantRepository,IProjectRepository projectRepository
            , IUserProjectRepository userProjectRepository,
            IPinnedItemRepository pinnedItemRepository
            ,IIssueRepository issueRepository
            , IMapper mapper

            , AppDbContext dbContext, IUserRepository userRepository, ISprintRepository sprintRepository)
        {
            _UserTenantRepository = UserTenantRepository;
            _DashBoardRepository = DashBoardRepository;
            this._response = new APIResponse();
            _TenantRepository = tenantRepository;
            _ProjectRepository = projectRepository;
            _UserProjectRepository = userProjectRepository;
            _dbContext = dbContext;
            _UserRepository = userRepository;
            _PinnedItemRepository = pinnedItemRepository;
            _SprintRepository = sprintRepository;
            _IssueRepository = issueRepository;
            _mapper = mapper;
        }
        [Authorize]
        [HttpGet("Tenants")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetTenantAnalysis([FromQuery] int Tenantid)
        {
            try
            {
                if (Tenantid == 0)
                {
                    return BadRequest("ID is invalid");
                }
                var tenant = await _TenantRepository.GetAsync(t => t.Id == Tenantid);

                if (tenant == null)
                {
                    return BadRequest("There is no tenant with this id");

                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                int parsedUserId = int.Parse(userId);
                var usertenant = await _UserTenantRepository.GetAsync(t => t.UserId == parsedUserId && t.TenantId == Tenantid);
                if (usertenant == null)
                {
                    return BadRequest("This User no found in this tenant");

                }
                _response.Result = await _DashBoardRepository.GetAnalysisTenantsSummaryAsync(Tenantid, parsedUserId);
                _response.IsSuccess = true;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [Authorize]
        [HttpGet("Projects")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetProjectAnalysis([FromQuery] int Projectid)
        {
            try
            {
                if (Projectid == 0)
                {
                    return BadRequest("ID is invalid");
                }
                var project = await _ProjectRepository.GetAsync(p => p.Id ==Projectid);

                if (project == null)
                {
                    return BadRequest("There is no project with this id");

                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                int parsedUserId = int.Parse(userId);
                var userproject = await _UserProjectRepository.GetAsync(t => t.UserId == parsedUserId && t.ProjectId == Projectid);
                if (userproject == null)
                {
                    return BadRequest("This User no found in this project");

                }
                _response.Result = await _DashBoardRepository.GetAnalysisProjectsSummaryAsync(Projectid, parsedUserId);
                _response.IsSuccess = true;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }

















        [Authorize]

        [HttpGet("allproject")]
        public async Task<ActionResult<APIResponse>> GetprojectDashboard()
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    return Unauthorized("Invalid or missing User ID.");
                }

                //var userTenant = await _UserTenantRepository.GetAsync(t => t.UserId == parsedUserId && t.TenantId == Tenantid);
                //if (userTenant == null)
                //{
                //    return BadRequest("This user is not assigned to this tenant.");
                //}
                var Projects = await _DashBoardRepository.GetProjectsDashboard(parsedUserId);
                _response.Result = Projects;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server Error: {ex}");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }

        [Authorize]
        [HttpGet("allissue")]
        public async Task<ActionResult<APIResponse>> GetissueDashboard()
        {
            try
            { 
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    return Unauthorized("Invalid or missing User ID.");
                }

                //var userTenant = await _UserTenantRepository.GetAsync(t => t.UserId == parsedUserId && t.TenantId == Tenantid);
                //if (userTenant == null)
                //{
                //    return BadRequest("This user is not assigned to this tenant.");
                //}
                var issues = await _DashBoardRepository.GetIssuesDashboard(parsedUserId);
                _response.Result = issues;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server Error: {ex}");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }


        [Authorize]
        [HttpGet("Calender")]
        
        public async Task<ActionResult<APIResponse>> GetCalendar()
        {
           
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out int parsedUserId))
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Invalid or missing User ID." };
                    return Unauthorized(_response);
                }

                var result = await _DashBoardRepository.GetUserIssuesTimeline(parsedUserId);

                if (result == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "No data found." };
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {

                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "An internal server error occurred." };
                return StatusCode(500, _response);
            }
        }

        [Authorize]
        [HttpGet("Pinneditems")]

        public async Task<ActionResult<APIResponse>> GetPinnedItems()
        {
            try
            {
                
                if (!int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out int userId))
                    return Unauthorized(new { message = "Invalid token" });

                
                var user = await _UserRepository.GetAsync(u => u.Id == userId);
                if (user == null)
                    return BadRequest(new { message = "User not found" });

                var pinnedItems = await _PinnedItemRepository.GetAllAsync(p => p.UserId == userId);
                if (!pinnedItems.Any())
                    return NotFound(new { message = "No pinned items found" });

                var tenants = new List<TenantDTO>();
                var projects = new List<ProjectDTO>();
                var sprints = new List<SprintDTO>();
                var issues = new List<IssueDTO>();

                foreach (var pinnedItem in pinnedItems)
                {
                    if (pinnedItem.ItemType == "Tenant")
                    {
                        var tenant = await _TenantRepository.GetAsync(t => t.Id == pinnedItem.ItemId);
                        if (tenant != null)
                            tenants.Add(_mapper.Map<TenantDTO>(tenant));
                    }
                    else if (pinnedItem.ItemType == "Project")
                    {
                        var project = await _ProjectRepository.GetAsync(p => p.Id == pinnedItem.ItemId);
                        if (project != null)
                            projects.Add(_mapper.Map<ProjectDTO>(project));
                    }
                    else if (pinnedItem.ItemType == "Sprint")
                    {
                        var sprint = await _SprintRepository.GetAsync(s => s.Id == pinnedItem.ItemId);
                        if (sprint != null)
                            sprints.Add(_mapper.Map<SprintDTO>(sprint));
                    }
                    else if (pinnedItem.ItemType == "Issue")
                    {
                        var issue = await _IssueRepository.GetAsync(i => i.Id == pinnedItem.ItemId);
                        if (issue != null)
                            issues.Add(_mapper.Map<IssueDTO>(issue));
                    }
                }

                
                _response.Result = new
                {
                    Tenants = tenants,
                    Projects = projects,
                    Sprints = sprints,
                    Issues = issues
                };
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }







    }
}