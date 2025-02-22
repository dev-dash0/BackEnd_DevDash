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
        private ITenantRepository _tenant;
        private IProjectRepository _project;
        private IUserRepository _user;
        private IIssueRepository _issue;
        private IUserProjectRepository _userProject;
        private IUserTenantRepository _userTenant;
        private IIssueAssignUserRepository _issueAssignedUser;
        private readonly IMapper _mapper;
        private APIResponse _response;
        public IssueAssignedUserController(IUserProjectRepository userProject, ITenantRepository tenant,
         IUserRepository user, IMapper mapper, IProjectRepository project, IUserTenantRepository userTenant,
         IIssueAssignUserRepository issueAssignedUser, IIssueRepository issue)
        {
            this._response = new APIResponse();
            _tenant = tenant;
            _project = project;
            _mapper = mapper;
            _user = user;
            _userProject = userProject;
            _issueAssignedUser = issueAssignedUser;
            _userTenant = userTenant;
            _issue = issue;
        }
        [HttpPost]
        public async Task<ActionResult<APIResponse>> JoinIssue(JoinIssueDTO joinIssue)
        {
            try
            { 
            if(joinIssue == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Data is Empty!");
                    return BadRequest(ModelState);
                }
                var user = await _user.GetAsync(u => u.Id == joinIssue.userId);
            if (user == null)
            {
                ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                return BadRequest(ModelState);
            }

            var Issue = await _issue.GetAsync(I => I.Id == joinIssue.issueId);
            if (Issue == null)
            {
                ModelState.AddModelError("ErrorMessages", "Issue is Invalid!");
                return BadRequest(ModelState);
            }
            var Project = await _project.GetAsync(p => p.Id == Issue.ProjectId);
            if(Project==null)
            {
                ModelState.AddModelError("ErrorMessages", "Project is Invalid!");
                return BadRequest(ModelState);
            }
            //check if this user found on this Issue
            var userproject = await _userProject.GetAsync(uI => uI.UserId == user.Id && uI.ProjectId == Project.Id);
            if (userproject == null)
            {
                ModelState.AddModelError("ErrorMessages", "User not found on project");
                return BadRequest(ModelState);
            }

            var existingUserIssue=await _issueAssignedUser
                   .GetAsync(ut => ut.UserId == user.Id && ut.IssueId == Issue.Id);

             

                if (existingUserIssue != null)
            {
                ModelState.AddModelError("ErrorMessages", "User is already a member of this Issue!");
                return BadRequest(ModelState);
            }
            var tenant = await _tenant.GetAsync(t => t.Id==Project.TenantId);
            if (tenant == null)
            {
                ModelState.AddModelError("ErrorMessages", "Tenant is Invalid!");
                return BadRequest(ModelState);
            }
            var existingUserTenant = await _userTenant.GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);
            if(existingUserTenant==null)
            {
                
                    ModelState.AddModelError("ErrorMessages", "User not Assigned in Tenant");
                    return BadRequest(ModelState);
            }
            IssueAssignedUserDTO issueAssignedUserDTO = new ()
            {
                userId = user.Id,
                issueId = Issue.Id,
                Assign_date= DateTime.Now
            };
            IssueAssignedUser issueAssignedUser = _mapper.Map<IssueAssignedUser>(issueAssignedUserDTO);
            await  _issueAssignedUser.JoinAsync(issueAssignedUser);
            _response.IsSuccess = true;
            _response.Result = issueAssignedUserDTO;
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

        [HttpDelete]
        public async Task<ActionResult<APIResponse>> LeaveIssue(LeaveIssueDTO leaveIssueDTO)
        {
            try
            {
                var user = await _user.GetAsync(u => u.Id == leaveIssueDTO.userId);
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var Issue = await _issue.GetAsync(I => I.Id == leaveIssueDTO.issueId);
                if (Issue == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Issue is Invalid!");
                    return BadRequest(ModelState);
                }
                var Project = await _project.GetAsync(p => p.Id == Issue.ProjectId);
                if (Project == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Project is Invalid!");
                    return BadRequest(ModelState);
                }
                //check if this user found on this Issue
                var userproject = await _userProject.GetAsync(uI => uI.UserId == user.Id && uI.ProjectId == Project.Id);
                if (userproject == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User not found on project");
                    return BadRequest(ModelState);
                }
                var existingUserIssue = await _issueAssignedUser
                       .GetAsync(ut => ut.UserId == user.Id && ut.IssueId == Issue.Id);

                if (existingUserIssue != null)
                {
                    ModelState.AddModelError("ErrorMessages", "User is already a member of this Issue!");
                    return BadRequest(ModelState);
                }
                var tenant = await _tenant.GetAsync(t => t.Id == Project.TenantId);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant is Invalid!");
                    return BadRequest(ModelState);
                }
                var existingUserTenant = await _userTenant.GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);
                if (existingUserTenant == null)
                {

                    ModelState.AddModelError("ErrorMessages", "User not Assigned in Tenant");
                    return BadRequest(ModelState);
                }

                _issueAssignedUser.LeaveAsync(existingUserIssue);
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
