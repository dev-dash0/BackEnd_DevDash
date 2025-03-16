using AutoMapper;
using DevDash.DTO;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.DTO.Sprint;
using DevDash.DTO.Tenant;
using DevDash.model;
using DevDash.Repository;
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
        private readonly APIResponse _response;

        public PinnedItemController(IPinnedItemRepository pinnedItemRepository, IUserRepository userRepository,
            ITenantRepository tenantRepository, IProjectRepository projectRepository,
            ISprintRepository sprintRepository
            , IUserProjectRepository userProjectRepository
            , IIssueRepository issueRepository,IUserTenantRepository userTenantRepository, IMapper mapper, IIssueAssignUserRepository issueAssignUserRepository)
        {
            _pinnedItemRepository = pinnedItemRepository;
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _projectRepository = projectRepository;
            _userTenantRepository = userTenantRepository;
            _userProjectRepository = userProjectRepository;
            _sprintRepository = sprintRepository;
            _issueRepository = issueRepository;
            _mapper = mapper;
            _response = new APIResponse();
            _issueAssignUserRepository = issueAssignUserRepository;

        }

        [HttpPost("pin")]
        public async Task<ActionResult<APIResponse>> PinItem([FromQuery] string itemType, [FromQuery] int itemId)
        {
            try
            {

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });
                if(itemType != "Tenant" && itemType != "tenant" && itemType != "Project" && itemType != "project" 
                    && itemType != "Sprint" && itemType != "sprint"
&& itemType != "Issue" && itemType != "issue"
                    )
                        {
                    return BadRequest(new { message = "Error in ItemType" });
                }
                {

                }

                if (itemType == "Tenant" || itemType == "tenant")

                {
                    var item =await _userTenantRepository.GetAsync(t => t.TenantId == itemId && int.Parse(userId) == t.UserId);
                    if (item == null)
                    {
                        return BadRequest(new { message = "User is not a member of this tenant" });
                    }
                }
                else if (itemType == "Project" || itemType == "project")

                {
                    var item =await _userProjectRepository.GetAsync(t => t.ProjectId == itemId && int.Parse(userId) == t.UserId);
                    if (item == null)
                    {
                        return BadRequest(new { message = "User is not a member of this project" });
                    }
                }
                else if(itemType== "Sprint" || itemType == "sprint")
                {

                    Sprint item = await _sprintRepository.GetAsync(t => t.Id == itemId);
                    if (item == null)
                    {
                        return BadRequest(new { message = "Sprint not found" });
                    }

                    var actualitem = await _userProjectRepository.GetAsync(t => t.ProjectId == item.ProjectId && int.Parse(userId) == t.UserId);
                    if (actualitem == null)
                    {
                        return BadRequest(new { message = "User is not a member of this Sprint" });
                    }

                }
                else if(itemType== "Issue" || itemType == "issue")
                {
                    var item = await _issueAssignUserRepository.GetAsync(t => t.IssueId == itemId && int.Parse(userId) == t.UserId);
                    if (item == null)
                    {
                        return BadRequest(new { message = "User is not assigned to this issue" });
                    }
                }



                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                    return BadRequest(new { message = "User not found" });

                var existingPin = await _pinnedItemRepository.GetAsync(p => p.UserId == user.Id && p.ItemId == itemId && p.ItemType == itemType);
                if (existingPin != null)
                {
                    return BadRequest(new { message = "Item is already pinned" });
                }

                PinnedItem pinnedItem = new()
                {
                    UserId = user.Id,
                    ItemId = itemId,
                    ItemType = itemType,
                    PinnedDate = DateTime.UtcNow
                };

                await _pinnedItemRepository.JoinAsync(pinnedItem);
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

        [HttpDelete("unpin")]
        public async Task<ActionResult<APIResponse>> UnpinItem([FromQuery] string itemType, [FromQuery] int itemId)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                    return BadRequest(new { message = "User not found" });

                var pinnedItem = await _pinnedItemRepository.GetAsync(p => p.UserId == user.Id && p.ItemId == itemId && p.ItemType == itemType);
                if (pinnedItem == null)
                {
                    return NotFound(new { message = "Item is not pinned" });
                }

                await _pinnedItemRepository.LeaveAsync(pinnedItem);

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

        [HttpGet("show-pinned")]
        public async Task<ActionResult<APIResponse>> ShowPinnedItems([FromQuery] string itemType)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                    return BadRequest(new { message = "User not found" });

                var validTypes = new HashSet<string> { "tenant", "project", "sprint", "issue" };
                if (!validTypes.Contains(itemType.ToLower()))
                    return BadRequest(new { message = "Invalid item type" });

                var pinnedItems = await _pinnedItemRepository
                    .GetAllAsync(p => p.UserId == user.Id && p.ItemType == itemType);

                if (!pinnedItems.Any())
                    return NotFound(new { message = "No pinned items found" });

                var tasks = pinnedItems.Select(async pinnedItem =>
                {
                    var dto = itemType.ToLower() switch
                    {
                        "tenant" => _mapper.Map<TenantDTO>(await _tenantRepository.GetAsync(t => t.Id == pinnedItem.ItemId)),
                        "project" => _mapper.Map<ProjectDTO>(await _projectRepository.GetAsync(p => p.Id == pinnedItem.ItemId)),
                        "sprint" => _mapper.Map<SprintDTO>(await _sprintRepository.GetAsync(s => s.Id == pinnedItem.ItemId)),
                        "issue" => _mapper.Map<IssueDTO>(await _issueRepository.GetAsync(i => i.Id == pinnedItem.ItemId)),
                        _ => (object)null!
                    };
                    return dto;
                });

                var dtoList = (await Task.WhenAll(tasks)).Where(dto => dto != null).ToList();

                _response.Result = dtoList;
                _response.StatusCode = HttpStatusCode.OK;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
            }
            return _response;
        }




    }
}
