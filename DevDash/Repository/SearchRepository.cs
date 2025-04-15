using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class SearchRepository : ISearchRepository
    {
        private readonly AppDbContext _db;


        public SearchRepository(AppDbContext db)
        {
            _db = db;
        }


        public async Task<List<dynamic>> GlobalSearchIssues(string query, int userId)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<dynamic>();
            }

            query = query.ToLower();

            var issues = await _db.IssueAssignedUsers
                .Where(ia => ia.UserId == userId)
                .Select(ia => ia.Issue)
                .Where(issue =>
                    issue.Title.ToLower().Contains(query) ||
                    issue.Type.ToLower().Contains(query) ||
                    issue.Status.ToLower().Contains(query) ||
                    issue.Description.ToLower().Contains(query) ||
                    issue.Priority.ToLower().Contains(query)
                )
                .Select(issue => new
                {
                    issue.Title,
                    issue.Status,
                    issue.Priority,
                    ProjectName = issue.Project.Name,
                    TenantName = issue.Tenant.Name
                })
                .ToListAsync();

            return issues.Cast<dynamic>().ToList();
        }

        public async Task<List<dynamic>> GlobalSearchTenants(string query, int userId)
        {
            var tenants = await _db.Tenants
    .Where(tenant =>
        tenant.UserTenants.Any(ut => ut.UserId == userId) &&
        (tenant.Name.ToLower().Contains(query) ||
         tenant.Description.ToLower().Contains(query) ||
         tenant.Keywords.ToLower().Contains(query) ||
         tenant.TenantCode.ToLower().Contains(query))
    )
    .Select(tenant => new
    {
        tenant.Id,
        tenant.Name,
        tenant.Description,
        tenant.TenantCode
    })
    .ToListAsync();
            return tenants.Cast<dynamic>().ToList();
        }

        public async Task<List<dynamic>> GlobalSearchProjects(string query, int userId)
        {
            var projects = await _db.Projects
            .Where(project =>
                project.UserProjects.Any(up => up.UserId == userId) &&
                (project.Name.ToLower().Contains(query) ||
                project.Description.ToLower().Contains(query) ||
                project.Priority.ToLower().Contains(query) ||
                project.ProjectCode.ToLower().Contains(query))
            )
            .Select(project => new
            {
                project.Id,
                project.Name,
                project.Description,
                project.Priority,
                project.ProjectCode,
                TenantName = project.Tenant.Name

            })
            .ToListAsync();
            return projects.Cast<dynamic>().ToList();
        }
        public async Task<List<dynamic>> GlobalSearchSprints(string query, int userId)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<dynamic>();
            }

            query = query.ToLower(); // Convert query to lowercase once

            var sprints = await _db.Sprints
                .AsNoTracking() // Improve performance by disabling tracking
                .Where(sprint =>
                    sprint.Issues.Any(issue => issue.IssueAssignedUsers.Any(ia => ia.UserId == userId)) &&
                    (sprint.Title.ToLower().Contains(query) ||
                     sprint.Status.ToLower().Contains(query) ||
                     sprint.Description.ToLower().Contains(query) ||
                     sprint.Summary.ToLower().Contains(query))
                )
                .Select(sprint => new
                {
                    sprint.Id,
                    sprint.Title,
                    sprint.Status,
                    sprint.Description,
                    sprint.Summary,
                    ProjectName = sprint.Project != null ? sprint.Project.Name : "No Project"
                })
                .ToListAsync();

            return sprints.Cast<dynamic>().ToList(); // Efficient casting
        }


    }
}
