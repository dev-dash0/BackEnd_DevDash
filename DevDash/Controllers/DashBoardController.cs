using DevDash.DTO.Account;
using DevDash.DTO.Project;
using DevDash.Migrations;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashBoardController : ControllerBase
    {
        private readonly IDashBoardRepository _DashBoardRepository;
        private readonly ITenantRepository _TenantRepository;
        private readonly AppDbContext _dbContext;
        private readonly IUserTenantRepository _UserTenantRepository;
        private APIResponse _response;

        public DashBoardController(IDashBoardRepository DashBoardRepository, ITenantRepository tenantRepository
            , IUserTenantRepository UserTenantRepository, AppDbContext dbContext)
        {
            _UserTenantRepository = UserTenantRepository;
            _DashBoardRepository = DashBoardRepository;
            this._response = new APIResponse();
            _TenantRepository = tenantRepository;
            _dbContext = dbContext;
        }
        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAnalysis([FromQuery] int Tenantid)
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
                _response.Result = await _DashBoardRepository.GetAnalysisSummaryAsync(Tenantid, parsedUserId);
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
        public async Task<ActionResult<APIResponse>> GetprojectDashboard(int Tenantid)
        {
            try
            {
                if (Tenantid <= 0)
                {
                    return BadRequest("Tenant ID is invalid.");
                }

                var tenant = await _TenantRepository.GetAsync(t => t.Id == Tenantid);
                if (tenant == null)
                {
                    return NotFound("No tenant found with this ID.");
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    return Unauthorized("Invalid or missing User ID.");
                }

                var userTenant = await _UserTenantRepository.GetAsync(t => t.UserId == parsedUserId && t.TenantId == Tenantid);
                if (userTenant == null)
                {
                    return BadRequest("This user is not assigned to this tenant.");
                }
                var Projects = await _DashBoardRepository.GetProjectsDashboard(Tenantid, parsedUserId);
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
        public async Task<ActionResult<APIResponse>> GetissueDashboard(int Tenantid)
        {
            try
            {
                if (Tenantid <= 0)
                {
                    return BadRequest("Tenant ID is invalid.");
                }
                var tenant = await _TenantRepository.GetAsync(t => t.Id == Tenantid);
                if (tenant == null)
                {
                    return NotFound("No tenant found with this ID.");
                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    return Unauthorized("Invalid or missing User ID.");
                }

                var userTenant = await _UserTenantRepository.GetAsync(t => t.UserId == parsedUserId && t.TenantId == Tenantid);
                if (userTenant == null)
                {
                    return BadRequest("This user is not assigned to this tenant.");
                }
                var issues = await _DashBoardRepository.GetIssuesDashboard(Tenantid, parsedUserId);
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
                var userIdClaim =User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int parsedUserId))
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Invalid or missing User ID." };
                    return Unauthorized(_response);
                }
                var pinnedtenantsItems = await _DashBoardRepository.GetUserPinnedtenant(parsedUserId);
                var pinnedprojectItems = await _DashBoardRepository.GetUserPinnedproject(parsedUserId);
                var pinnedsprintsItems = await _DashBoardRepository.GetUserPinnedsprint(parsedUserId);
                var pinnedIssuesItems = await _DashBoardRepository.GetUserPinnedissue(parsedUserId);
              
                if (pinnedIssuesItems == null && pinnedprojectItems==null && pinnedsprintsItems==null && pinnedtenantsItems==null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "No data found." };
                    return NotFound(_response);
                }
                _response.IsSuccess = true;
                _response.Result = new
                {
                    Tenants=pinnedtenantsItems,
                    projects = pinnedprojectItems,
                    sprints=pinnedsprintsItems,
                    issues = pinnedIssuesItems
                };
                return Ok(_response);
            }
            catch (Exception ex)
            {

                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "An internal server error occurred." };
                return StatusCode(500, _response);
            }
        }


    }
}