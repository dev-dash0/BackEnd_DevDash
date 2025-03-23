using AutoMapper;
using DevDash.DTO;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.DTO.Sprint;
using DevDash.DTO.Tenant;
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
    public class PinnedItemController : ControllerBase
    {
        private readonly IPinnedItemRepository _pinnedItemRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ISprintRepository _sprintRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly IUserTenantRepository _userTenantRepository;
        private readonly IUserProjectRepository _userProjectRepository;
        private readonly IIssueAssignUserRepository _issueAssignUserRepository;
        private readonly IMapper _mapper;

        public PinnedItemController(IPinnedItemRepository pinnedItemRepository, IUserRepository userRepository,
            ITenantRepository tenantRepository, IProjectRepository projectRepository,
            ISprintRepository sprintRepository, IUserProjectRepository userProjectRepository,
            IIssueRepository issueRepository, IUserTenantRepository userTenantRepository,
            IMapper mapper, IIssueAssignUserRepository issueAssignUserRepository)
        {
            _pinnedItemRepository = pinnedItemRepository;
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _projectRepository = projectRepository;
            _sprintRepository = sprintRepository;
            _issueRepository = issueRepository;
            _userTenantRepository = userTenantRepository;
            _userProjectRepository = userProjectRepository;
            _issueAssignUserRepository = issueAssignUserRepository;
            _mapper = mapper;
        }

        [HttpPost("pin")]
        public async Task<ActionResult<APIResponse>> PinItem([FromQuery] string itemType, [FromQuery] int itemId)
        {
            var response = new APIResponse();
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Invalid token" });

                if (!IsValidItemType(itemType)) return BadRequest(new { message = "Invalid item type" });

                if (!await UserHasAccessToItem(int.Parse(userId), itemType, itemId))
                    return BadRequest(new { message = "User does not have access to this item" });

                var existingPin = await _pinnedItemRepository.GetAsync(p => p.UserId == int.Parse(userId) && p.ItemId == itemId && p.ItemType == itemType);
                if (existingPin != null) return BadRequest(new { message = "Item is already pinned" });

                var pinnedItem = new PinnedItem { UserId = int.Parse(userId), ItemId = itemId, ItemType = itemType, PinnedDate = DateTime.UtcNow };
                await _pinnedItemRepository.JoinAsync(pinnedItem);

                response.StatusCode = HttpStatusCode.Created;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, response);
            }
        }

        [HttpDelete("unpin")]
        public async Task<ActionResult<APIResponse>> UnpinItem([FromQuery] string itemType, [FromQuery] int itemId)
        {
            var response = new APIResponse();
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Invalid token" });

                var pinnedItem = await _pinnedItemRepository.GetAsync(p => p.UserId == int.Parse(userId) && p.ItemId == itemId && p.ItemType == itemType);
                if (pinnedItem == null) return NotFound(new { message = "Item is not pinned" });

                await _pinnedItemRepository.LeaveAsync(pinnedItem);

                response.StatusCode = HttpStatusCode.NoContent;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, response);
            }
        }

        [HttpGet("owned-pinned-items")]
        public async Task<ActionResult<APIResponse>> GetOwnedPinnedItems([FromQuery] string itemType)
        {
            var response = new APIResponse();
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Invalid token" });

                if (!IsValidItemType(itemType)) return BadRequest(new { message = "Invalid item type" });

                var pinnedItems = await _pinnedItemRepository.GetAllAsync(p => p.UserId == int.Parse(userId) && p.ItemType == itemType);
                var itemIds = pinnedItems.Select(p => p.ItemId).ToList();

                object result = itemType.ToLower() switch
                {
                    "tenant" => _mapper.Map<IEnumerable<TenantDTO>>(await _tenantRepository.GetAllAsync(t => itemIds.Contains(t.Id))),
                    "project" => _mapper.Map<IEnumerable<ProjectDTO>>(await _projectRepository.GetAllAsync(p => itemIds.Contains(p.Id))),
                    "sprint" => _mapper.Map<IEnumerable<SprintDTO>>(await _sprintRepository.GetAllAsync(s => itemIds.Contains(s.Id))),
                    "issue" => _mapper.Map<IEnumerable<IssueDTO>>(await _issueRepository.GetAllAsync(i => itemIds.Contains(i.Id))),
                    _ => null
                };

                if (result == null) return BadRequest(new { message = "Invalid item type" });

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, response);
            }
        }

        private bool IsValidItemType(string itemType)
        {
            var validTypes = new HashSet<string> { "Tenant", "Project", "Sprint", "Issue" };
            return validTypes.Contains(itemType, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<bool> UserHasAccessToItem(int userId, string itemType, int itemId)
        {
            return itemType.ToLower() switch
            {
                "tenant" => await _userTenantRepository.GetAsync(t => t.TenantId == itemId && t.UserId == userId) != null,
                "project" => await _userProjectRepository.GetAsync(p => p.ProjectId == itemId && p.UserId == userId) != null,
                "sprint" => await SprintUserHasAccess(userId, itemId),
                "issue" => await _issueAssignUserRepository.GetAsync(i => i.IssueId == itemId && i.UserId == userId) != null,
                _ => false
            };
        }

        private async Task<bool> SprintUserHasAccess(int userId, int sprintId)
        {
            var sprint = await _sprintRepository.GetAsync(s => s.Id == sprintId);
            if (sprint == null) return false;

            return await _userProjectRepository.GetAsync(up => up.ProjectId == sprint.ProjectId && up.UserId == userId) != null;
        }
    }
}
