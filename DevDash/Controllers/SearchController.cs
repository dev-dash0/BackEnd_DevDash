using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DevDash.Controllers
{
    public class SearchController : ControllerBase
    {
        private readonly ISearchRepository _searchRepository;
        private APIResponse response;
        public SearchController(ISearchRepository searchRepository)
        {
            _searchRepository = searchRepository;
            response = new APIResponse();
        }

        [HttpGet("global")]
        public async Task<ActionResult<APIResponse>> GlobalSearch([FromQuery] string query)
        {
            try
            {
                var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId))
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var issues = await _searchRepository.GlobalSearchIssues(query, userId);
                var projects = await _searchRepository.GlobalSearchProjects(query, userId);
                var sprints = await _searchRepository.GlobalSearchSprints(query, userId);
                var tenants = await _searchRepository.GlobalSearchTenants(query, userId);

                if (issues == null || issues.Count == 0 && (projects == null || projects.Count == 0) && (sprints == null || sprints.Count == 0) && (tenants == null || tenants.Count == 0))
                {
                    response.IsSuccess = false;
                    response.ErrorMessages = new List<string> { "No results found." };
                    return NotFound(response);
                }
                var searchResults = new
                {
                    Issues = issues,
                    Projects = projects,
                    Tenants = tenants,
                    Sprints = sprints
                };

                response.IsSuccess = true;
                response.Result = searchResults;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages = new List<string> { "An internal server error occurred.", ex.Message };
                return StatusCode(500, response);
            }

        }
    }
}
