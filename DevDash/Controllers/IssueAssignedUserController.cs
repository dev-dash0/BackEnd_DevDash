using AutoMapper;
using DevDash.DTO.IssueAssignedUser;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DevDash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IssueAssignedUserController : ControllerBase
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly IUserProjectRepository _userProjectRepository;
        private readonly IUserTenantRepository _userTenantRepository;
        private readonly IIssueAssignUserRepository _issueAssignUserRepository;
        private readonly IMapper _mapper;
        private readonly APIResponse _response;

        public IssueAssignedUserController(
            IUserProjectRepository userProjectRepository,
            ITenantRepository tenantRepository,
            IUserRepository userRepository,
            IMapper mapper,
            IProjectRepository projectRepository,
            IUserTenantRepository userTenantRepository,
            IIssueAssignUserRepository issueAssignUserRepository,
            IIssueRepository issueRepository)
        {
            _response = new APIResponse();
            _tenantRepository = tenantRepository;
            _projectRepository = projectRepository;
            _mapper = mapper;
            _userRepository = userRepository;
            _userProjectRepository = userProjectRepository;
            _issueAssignUserRepository = issueAssignUserRepository;
            _userTenantRepository = userTenantRepository;
            _issueRepository = issueRepository;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> JoinIssue(JoinIssueDTO joinIssue)
        {
            try
            {
                if (joinIssue == null)
                    return BadRequest("Data is empty!");

                var (user, issue, project, tenant) = await GetIssueContextAsync(joinIssue.userId, joinIssue.issueId);
                if (user == null || issue == null || project == null || tenant == null)
                    return BadRequest("Invalid input data!");

                if (!await IsUserAssignedToProjectAsync(user.Id, project.Id))
                    return BadRequest("User not found in the project!");

                if (!await IsUserAssignedToTenantAsync(user.Id, tenant.Id))
                    return BadRequest("User not assigned to the tenant!");

                if (await _issueAssignUserRepository.GetAsync(ut => ut.UserId == user.Id && ut.IssueId == issue.Id) != null)
                    return BadRequest("User is already a member of this issue!");

                var issueAssignedUser = new IssueAssignedUser
                {
                    UserId = user.Id,
                    IssueId = issue.Id,
                };

                await _issueAssignUserRepository.JoinAsync(issueAssignedUser,joinIssue.userId.ToString());

                _response.IsSuccess = true;
                _response.Result = _mapper.Map<IssueAssignedUserDTO>(issueAssignedUser);
                _response.StatusCode = HttpStatusCode.Created;

                return _response;
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpDelete]
        public async Task<ActionResult<APIResponse>> LeaveIssue(LeaveIssueDTO leaveIssue)
        {
            try
            {
                if (leaveIssue == null)
                    return BadRequest("Invalid data!");

                var (user, issue, project, tenant) = await GetIssueContextAsync(leaveIssue.userId, leaveIssue.issueId);
                if (user == null || issue == null || project == null || tenant == null)
                    return BadRequest("Invalid input data!");

                if (!await IsUserAssignedToProjectAsync(user.Id, project.Id))
                    return BadRequest("User not found in the project!");

                if (!await IsUserAssignedToTenantAsync(user.Id, tenant.Id))
                    return BadRequest("User not assigned to the tenant!");

                var existingUserIssue = await _issueAssignUserRepository.GetAsync(ut => ut.UserId == user.Id && ut.IssueId == issue.Id);
                if (existingUserIssue == null)
                    return BadRequest("User is not assigned to this issue!");

                await _issueAssignUserRepository.LeaveAsync(existingUserIssue, leaveIssue.userId.ToString());

                _response.StatusCode = HttpStatusCode.NoContent;
                return _response;
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private async Task<(User?, Issue?, Project?, Tenant?)> GetIssueContextAsync(int userId, int issueId)
        {
            var user = await _userRepository.GetAsync(u => u.Id == userId);
            var issue = await _issueRepository.GetAsync(i => i.Id == issueId);
            var project = issue != null ? await _projectRepository.GetAsync(p => p.Id == issue.ProjectId) : null;
            var tenant = project != null ? await _tenantRepository.GetAsync(t => t.Id == project.TenantId) : null;

            return (user, issue, project, tenant);
        }

        private async Task<bool> IsUserAssignedToProjectAsync(int userId, int projectId) =>
            await _userProjectRepository.GetAsync(u => u.UserId == userId && u.ProjectId == projectId) != null;

        private async Task<bool> IsUserAssignedToTenantAsync(int userId, int tenantId) =>
            await _userTenantRepository.GetAsync(u => u.UserId == userId && u.TenantId == tenantId) != null;

        private ActionResult<APIResponse> HandleException(Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> { ex.Message };
            _response.StatusCode = HttpStatusCode.InternalServerError;
            return _response;
        }
    }
}
