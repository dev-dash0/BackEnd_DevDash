using AutoMapper;
using DevDash.DTO.Issue;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
                if (!int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out int userId))
                    return Unauthorized(new { message = "Invalid token" });


                var userProject = await _dbUserProject.GetAsync(up => up.UserId == userId && up.ProjectId == projectId);
                if (userProject == null) return Forbid();

                IEnumerable<Issue> Issues = new List<Issue>();

                if (userProject.Role == "Admin" || userProject.Role == "Project Manager" || userProject.Role== "Developer")
                {
                    Issues = await _dbissue.GetAllAsync(
                        filter: i => i.ProjectId == projectId && i.SprintId == null && i.IsBacklog &&
                                     (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                        includeProperties: "CreatedBy",
                        pageSize: pageSize,
                        pageNumber: pageNumber
                    );
                }
                //else if (userProject.Role == "Developer")
                //{
                //    Issues = await _dbissue.GetAllAsync(
                //        filter: i => i.ProjectId == projectId && i.SprintId == null && i.IsBacklog &&
                //                     i.IssueAssignedUsers.Any(iau => iau.UserId == userId) &&
                //                     (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                //        includeProperties: "",
                //        pageSize: pageSize,
                //        pageNumber: pageNumber
                //    );
                //}
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


                Issues = await _dbissue.GetAllAsync(
                    filter: i => i.SprintId == sprintId &&
                                 (string.IsNullOrEmpty(search) || i.Labels.ToLower().Contains(search.ToLower())),
                    includeProperties: "CreatedBy,AssignedUsers",
                    pageSize: pageSize,
                    pageNumber: pageNumber
                );
                 
                
               
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
                    includeProperties: "CreatedBy,AssignedUsers"
                );

                if (issue == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "Issue not found" };
                    return NotFound(_response);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == userIdInt && up.ProjectId == issue.ProjectId);
                if (userProject == null) return Forbid();

                //if (userProject.Role != "Admin" && userProject.Role != "Project Manager")
                //{
                //    bool isAssignedToUser = issue?.IssueAssignedUsers?.Any(iau => iau.UserId == userIdInt) == true;


                //    if (!isAssignedToUser)
                //    {
                //        return Forbid("User is not admin or Project Manager");
                //    }
                //}

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
        public async Task<ActionResult<APIResponse>> CreateBacklogIssue([FromQuery] int projectId, [FromForm] IssueCreataDTO createDTO)
        {
            try
            {
                if (createDTO == null)
                    return BadRequest(new { message = "IssueCreateDTO cannot be null" });

                if (!int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out int userId))
                    return Unauthorized(new { message = "Invalid token" });

                //var userProject = await _dbUserProject.GetAsync(up => up.UserId == userId && up.ProjectId == projectId);
                //if (userProject == null || (userProject.Role != "Admin" && userProject.Role != "Project Manager"))
                //    return Unauthorized(new { message = "You do not have permission to create an issue in this project" });

                Project project = await _dbProject.GetAsync(p => p.Id == projectId);
                if (project == null)
                    return BadRequest(new { message = "Project ID is Invalid!" });

                string attachmentUrl = null;
                if (createDTO.Attachment != null)
                {
                    List<string> validExtensions = new List<string> { ".jpg", ".png", ".jpeg", ".pdf", ".docx" };
                    string extension = Path.GetExtension(createDTO.Attachment.FileName);
                    if (!validExtensions.Contains(extension.ToLower()))
                    {
                        return BadRequest(new { message = "Invalid file type. Only .jpg, .png, .jpeg, .pdf, and .docx are allowed." });
                    }

                    string fileName = Guid.NewGuid().ToString() + extension;
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");
                    Directory.CreateDirectory(uploadsFolder);
                    string fullPath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await createDTO.Attachment.CopyToAsync(fileStream);
                    }

                   
                    attachmentUrl = $"http://devdash.runasp.net/attachments/{fileName}";
                }

                var issue = _mapper.Map<Issue>(createDTO);
                issue.IsBacklog = true;
                issue.ProjectId = projectId;
                issue.AttachmentPath = attachmentUrl;
                issue.CreatedById = userId;
                issue.TenantId = project.TenantId;
                issue.SprintId = null;
                issue.Status = "BackLog";

                await _dbissue.CreateAsync(issue);

                var issueDTO = _mapper.Map<IssueDTO>(issue);

                var issueAssignedUser = new IssueAssignedUser
                {
                    IssueId = issue.Id,
                    UserId = userId,
                };
                await _dbIssueAssignUser.JoinAsync(issueAssignedUser, userId);

                _response.Result = new
                {
                    id = issue.Id,
                    issue = issueDTO
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
        public async Task<ActionResult<APIResponse>> CreateSprintIssue([FromQuery] int sprintId, [FromForm] IssueCreataDTO createDTO)
        {
            try
            {
                if (createDTO == null)
                    return BadRequest(new { message = "IssueCreateDTO cannot be null" });

                if (!int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out int userId))
                    return Unauthorized(new { message = "Invalid token" });

                var sprint = await _dbSprint.GetAsync(s => s.Id == sprintId);
                if (sprint == null)
                    return BadRequest(new { message = "Sprint ID is Invalid!" });

                //var userProject = await _dbUserProject.GetAsync(up => up.UserId == userId && up.ProjectId == sprint.ProjectId);
                //if (userProject == null || (userProject.Role != "Admin" && userProject.Role != "Project Manager"))
                //    return Unauthorized(new { message = "You do not have permission to create an issue in this project" });

                string? attachmentUrl = null;
                if (createDTO.Attachment != null && createDTO.Attachment.Length > 0)
                {
                    List<string> validExtensions = new List<string> { ".jpg", ".png", ".jpeg", ".pdf", ".docx" };
                    string extension = Path.GetExtension(createDTO.Attachment.FileName);

                    if (!validExtensions.Contains(extension.ToLower()))
                        return BadRequest(new { message = "Invalid file type. Only .jpg, .png, .jpeg, .pdf, and .docx are allowed." });

                    string fileName = Guid.NewGuid().ToString() + extension;
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");
                    Directory.CreateDirectory(uploadsFolder);
                    string fullPath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await createDTO.Attachment.CopyToAsync(stream);
                    }

                    attachmentUrl = $"http://devdash.runasp.net/attachments/{fileName}";
                }

                var issue = _mapper.Map<Issue>(createDTO);
                issue.IsBacklog = false;
                issue.ProjectId = sprint.ProjectId;
                issue.SprintId = sprintId;
                issue.CreatedById = userId;
                issue.TenantId = sprint.TenantId;
                issue.AttachmentPath = attachmentUrl;

                await _dbissue.CreateAsync(issue);

                var issueAssignedUser = new IssueAssignedUser
                {
                    IssueId = issue.Id,
                    UserId = userId
                };
                await _dbIssueAssignUser.JoinAsync(issueAssignedUser, userId);

                var issueDTO = _mapper.Map<IssueDTO>(issue);
                _response.Result = new
                {
                    id = issue.Id,
                    issue = issueDTO
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
      
        public async Task<ActionResult<APIResponse>> UpdateIssue(int id, [FromForm] IssueUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string> { "Invalid issue ID or data" };
                    return BadRequest(_response);
                }

                var issue = await _dbissue.GetAsync(i => i.Id == id);
                if (issue == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "Issue not found" };
                    return NotFound(_response);
                }

                if (!int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string> { "Invalid token" };
                    return Unauthorized(_response);
                }

                var userProject = await _dbUserProject.GetAsync(up => up.UserId == userId && up.ProjectId == issue.ProjectId);
                if (userProject == null || (userProject.Role != "Admin" && userProject.Role != "Project Manager" && userProject.Role!= "Developer"))
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string> { "You do not have permission to update this issue" };
                    return Forbid();
                }

             
                updateDTO.IsBacklog = updateDTO.SprintId == null;

               
                if (updateDTO.Attachment != null && updateDTO.Attachment.Length > 0)
                {
                    List<string> validExtensions = new List<string> { ".jpg", ".png", ".jpeg", ".pdf", ".docx" };
                    string extension = Path.GetExtension(updateDTO.Attachment.FileName);

                    if (!validExtensions.Contains(extension.ToLower()))
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.ErrorMessages = new List<string> { "Invalid file type. Only .jpg, .png, .jpeg, .pdf, .docx allowed." };
                        return BadRequest(_response);
                    }

               
                    if (!string.IsNullOrEmpty(issue.AttachmentPath))
                    {
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", Path.GetFileName(issue.AttachmentPath));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

               
                    string fileName = Guid.NewGuid().ToString() + extension;
                    string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    Directory.CreateDirectory(savePath);
                    string fullPath = Path.Combine(savePath, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await updateDTO.Attachment.CopyToAsync(stream);
                    }

                   
                    issue.AttachmentPath = $"http://devdash.runasp.net/{fileName}";
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
