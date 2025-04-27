using AutoMapper;
using DevDash.DTO.Comment;
using DevDash.DTO.Sprint;
using DevDash.Migrations;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : Controller
    {
        private readonly ICommentRepository _dbComment;
        private readonly IIssueRepository _dbissue;
        private readonly IProjectRepository _dbProject;
        private readonly ISprintRepository _dbSprint;
        private readonly ITenantRepository _dbTenant;
        private readonly IUserRepository _dbUser;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public CommentController(ICommentRepository commentRepo, IIssueRepository dbissue, IProjectRepository dbProject, ISprintRepository dbSprint, ITenantRepository dbTenant
            , IMapper mapper, IUserRepository dbUser)
        {
            _dbComment = commentRepo;
            _mapper = mapper;

            _dbissue = dbissue;
            _dbTenant = dbTenant;
            _dbProject = dbProject;
            _dbSprint = dbSprint;
            this._response = new APIResponse();
            _dbUser = dbUser;
        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetComments([FromQuery] int IssueId,[FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                //IEnumerable<Comment> Comments = await _dbComment.GetAllAsync();
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                IEnumerable<Comment> Comments = await _dbComment.GetAllAsync(
                           filter: c => c.Issue.Id == IssueId && (string.IsNullOrEmpty(search) || c.Content.ToLower().Contains(search.ToLower())),
                           includeProperties: "Issue,CreatedBy",
                           pageSize: pageSize,
                           pageNumber: pageNumber
                       );
                _response.Result = _mapper.Map<List<CommentDTO>>(Comments);
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


        [HttpGet("{id:int}", Name = "GetComment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetComment(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                var Comment = await _dbComment.GetAsync(u => u.Id == id
                , includeProperties: "Issue,CreatedBy"
                );
              
                if (Comment == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<CommentDTO>(Comment);
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
        public async Task<ActionResult<APIResponse>> CreateComment([FromQuery] int issueid, [FromBody] CommentinputDTO commentinputDTO )
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if(userId==null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return Unauthorized(_response);
                }
                int parsedUserId = int.Parse(userId);
                var user = await _dbUser.GetAsync(U => U.Id==parsedUserId);
                if(user==null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return Unauthorized(_response);
                }

                Issue issue = await _dbissue.GetAsync(I => I.Id == issueid);
                if (issue == null)
                {
                    return BadRequest("not found issue");
                }
                 Sprint sprint= await _dbSprint.GetAsync(S => S.Id == issue.SprintId);

                Project project = await _dbProject.GetAsync(P => P.Id == sprint.ProjectId);
                if (project == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Project ID is Invalid!");
                    return BadRequest(ModelState);
                }
                Tenant tenant= await _dbTenant.GetAsync(T => T.Id == project.TenantId);

                if (tenant==null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant ID does not match the project's Tenant ID!");
                    return BadRequest(ModelState);
                }

                CommentCreateDTO createDTO = new CommentCreateDTO
                {
                    Content = commentinputDTO.Content,
                    IssueId = issueid,
                    ProjectId = project.Id,
                    SprintId = sprint.Id,
                    TenantId = tenant.Id,
                    CreatedById = parsedUserId


                };

                Comment comment = _mapper.Map<Comment>(createDTO);
                await _dbComment.CreateAsync(comment);

                _response.Result = new
                {
                    Id = comment.Id, 
                    Comment = _mapper.Map<CommentDTO>(comment),
                
                }; ;
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetComment", new { id = comment.Id }, _response);
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
        [HttpDelete("{id:int}", Name = "DeleteComment")]
        public async Task<ActionResult<APIResponse>> DeleteComment(int id)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return Unauthorized(_response);
                }
                int parsedUserId = int.Parse(userId);
                var user = await _dbUser.GetAsync(U => U.Id == parsedUserId);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return Unauthorized(_response);
                }
             
                if (id == 0)
                {
                    return BadRequest();
                }
                var comment = await _dbComment.GetAsync(u => u.Id == id);
                if (comment == null)
                {
                    return NotFound();
                }
                if (user.Id != comment.CreatedById)
                {
                    return Unauthorized("You are not create this comment");
                }

                await _dbComment.RemoveAsync(comment);
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

        [HttpPut("{id:int}", Name = "UpdateComment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateComment(int id, [FromBody] CommentUpdateDTO updateDTO)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return Unauthorized(_response);
                }

                if (!int.TryParse(userId, out int parsedUserId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return Unauthorized(_response);
                }

                var user = await _dbUser.GetAsync(U => U.Id == parsedUserId);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return Unauthorized(_response);
                }

                if (updateDTO == null || id != updateDTO.Id)
                {
                    return BadRequest();
                }

                var comment = await _dbComment.GetAsync(c => c.Id == updateDTO.Id);
                if (comment == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Comment ID is Invalid!");
                    return BadRequest(ModelState);
                }
                if (user.Id != comment.CreatedById)
                {
                    return Unauthorized("You are not create this comment");
                }


                Issue issue = await _dbissue.GetAsync(I => I.Id == comment.IssueId);
                if (issue == null)
                {
                    return BadRequest("Not found issue");
                }

                Sprint sprint = await _dbSprint.GetAsync(S => S.Id == issue.SprintId);
                if (sprint == null)
                {
                    return BadRequest("Sprint not found");
                }

                Project project = await _dbProject.GetAsync(P => P.Id == sprint.ProjectId);
                if (project == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Project ID is Invalid!");
                    return BadRequest(ModelState);
                }

                Tenant tenant = await _dbTenant.GetAsync(T => T.Id == project.TenantId);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant ID does not match the project's Tenant ID!");
                    return BadRequest(ModelState);
                }

               
                _mapper.Map(updateDTO, comment);
                comment.IssueId = issue.Id;
                comment.ProjectId = project.Id;
                comment.SprintId = sprint.Id;
                comment.TenantId = tenant.Id;
                comment.CreationDate = DateTime.UtcNow;

                await _dbComment.UpdateAsync(comment);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<CommentDTO>(comment);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }






    }
}
