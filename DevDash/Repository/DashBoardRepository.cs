using AutoMapper;
using DevDash.DTO.DashBoard;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.DTO.Sprint;
using DevDash.DTO.Tenant;
using DevDash.DTO.User;
using DevDash.Migrations;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace DevDash.Repository
{
    public class DashBoardRepository : IDashBoardRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public DashBoardRepository(AppDbContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }
        public async Task<DashBoardTenantsDTO> GetAnalysisTenantsSummaryAsync(int tenantId, int? userId, string? includeProperties = null)
        {
            if (userId == null)
            {
                return new DashBoardTenantsDTO
                {
                    TotalProjects = 0,
                    CompletedProjects = 0,
                    ProjectsInProgress = 0,
                    ProjectsOverdue = 0
                };
            }

            var projectsQuery = _context.Projects
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId);

            var userProjectIds = await _context.UserProjects
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => up.ProjectId)
                .ToListAsync();

            var filteredProjects = await projectsQuery
                .Where(p => userProjectIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Status })
                .ToListAsync();

            var filteredProjectIds = filteredProjects.Select(p => p.Id).ToList();
            //var totalProjects = filteredProjectIds.Count;

            var completedProjects = filteredProjects.Count(p => p.Status == "Completed");
            var projectsInProgress = filteredProjects.Count(p => p.Status == "Working on");
            var projectsOverdue = filteredProjects.Count(p => p.Status == "Postponed");




            var totalProjects = completedProjects + projectsInProgress + projectsOverdue;

            return new DashBoardTenantsDTO
            {
                TotalProjects = totalProjects,
                CompletedProjects = completedProjects,
                ProjectsInProgress = projectsInProgress,
                ProjectsOverdue = projectsOverdue
            };
        }

        public async Task<List<ProjectDashBoardDTO>> GetProjectsDashboard(int userId, string? includeProperties = null)
        {
           
            var userProjectIds = await _context.UserProjects
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => up.ProjectId)
                .ToListAsync();

            if (!userProjectIds.Any())
            {
                return new List<ProjectDashBoardDTO>();
            }

            var filteredProjects = await _context.Projects
             .AsNoTracking()
            .Where(p => userProjectIds.Contains(p.Id))
            .Include(p => p.Tenant)
            .Include(p => p.Tenant.JoinedUsers)
             .Include(p => p.Creator)
            .Include(p => p.UserProjects)
              .ThenInclude(up => up.User)
              .ToListAsync();
            var projects = _mapper.Map<List<ProjectDashBoardDTO>>(filteredProjects);
            return projects;
        }




        public async Task<List<IssueDashBoardDTO>> GetIssuesDashboard(int userId)
        {

            var userIssueIds = await _context.IssueAssignedUsers
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => up.IssueId)
                .ToListAsync();

            if (!userIssueIds.Any())
            {
                return new List<IssueDashBoardDTO>();
            }
            var filteredIssues = await _context.Issues
          .AsNoTracking()
          .Where(issue => userIssueIds.Contains(issue.Id))
          .Select(issue => new IssueDashBoardDTO
          {
              Id = issue.Id,
              Title = issue.Title,
              Description = issue.Description,
              Status = issue.Status,
              Priority = issue.Priority,
              ProjectId = issue.ProjectId,
              Deadline = issue.Deadline,
              TenantName = _context.Tenants
                  .Where(t => t.Id == issue.TenantId)
                  .Select(t => t.Name)
                  .FirstOrDefault() ?? "Unknown",
              ProjectName = _context.Projects
                  .Where(p => p.Id == issue.ProjectId)
                  .Select(p => p.Name)
                  .FirstOrDefault() ?? "Unknown"
          })
          .ToListAsync();
            var issues = _mapper.Map<List<IssueDashBoardDTO>>(filteredIssues);
            return issues;
        }


        ////////////////////////////////////
        ///

        public async Task<List<object>> GetUserIssuesTimeline(int userId)
        {
            var issues = await _context.Issues
                .AsNoTracking()
                .Where(issue => issue.IssueAssignedUsers.Any(ia => ia.UserId == userId))
                .Include(issue => issue.Project)
                .Include(issue => issue.Tenant)
                .OrderBy(issue => issue.Deadline)
                .ToListAsync();

            if (!issues.Any())
            {
                return null;
            }

            DateTime minStartDate = issues.Min(i => i.StartDate) ?? DateTime.UtcNow.Date;
            DateTime maxDeadline = issues.Max(i => i.Deadline) ?? DateTime.UtcNow.Date;

            var timeline = new List<object>();

            for (DateTime date = minStartDate; date <= maxDeadline; date = date.AddDays(1))
            {
                var issuesForDay = issues
                    .Where(issue => issue.StartDate <= date)
                    .OrderBy(issue => issue.Deadline)
                    .Select(issue => new
                    {
                        issue.Id,
                        issue.Title,
                        issue.Priority,
                        issue.Type,
                        ProjectName = issue.Project.Name,
                        TenantName = issue.Tenant.Name,
                        issue.StartDate,
                        issue.Deadline
                    })
                    .ToList();

                if (issuesForDay.Any())
                {
                    timeline.Add(new
                    {
                        date = date.ToString("yyyy-MM-dd"),
                        issues = issuesForDay
                    });
                }
            }


            return timeline.OrderBy(day =>
            {
                var issuesList = ((dynamic)day).issues as List<dynamic>;

                if (issuesList == null || !issuesList.Any())
                    return DateTime.MaxValue;

                return issuesList
                    .Select(issue =>
                    {
                        DateTime deadline;
                        return DateTime.TryParse(issue.Deadline, out deadline) ? deadline : DateTime.MaxValue;
                    })
                    .Min();
            }).ToList();

        }



        ///////////

        public async Task<List<Project>> GetUserPinnedProjects(int userId)
        {
            var pinnedProjectIds = await _context.PinnedItems
                .Where(pi => pi.UserId == userId && pi.ItemType == "Project")
                .Select(pi => (int)pi.ItemId) // Assuming ItemId stores the Project Id
                .ToListAsync();

            var pinnedProjects = await _context.Projects
                .Where(project => pinnedProjectIds.Contains(project.Id))
                .ToListAsync();

            return pinnedProjects;
        }

        public async Task<List<Issue>> GetUserPinnedIssues(int userId)
        {
            var pinnedIssueIds = await _context.PinnedItems
                .Where(pi => pi.UserId == userId && pi.ItemType == "Issue")
                .Select(pi => (int)pi.ItemId)
                .ToListAsync();

            var pinnedIssues = await _context.Issues
                .Where(issue => pinnedIssueIds.Contains(issue.Id))
                .ToListAsync();

            return pinnedIssues;
        }

        public async Task<List<Sprint>> GetUserPinnedSprints(int userId)
        {
            var pinnedSprintIds = await _context.PinnedItems
                .Where(pi => pi.UserId == userId && pi.ItemType == "Sprint")
                .Select(pi => (int)pi.ItemId) // Assuming ItemId stores the Sprint Id
                .ToListAsync();

            var pinnedSprints = await _context.Sprints
                .Where(sprint => pinnedSprintIds.Contains(sprint.Id) &&
                                 _context.UserProjects.Any(pu => pu.ProjectId == sprint.ProjectId && pu.UserId == userId))
                .ToListAsync();

            return pinnedSprints;
        }

        public async Task<List<Tenant>> GetUserPinnedTenants(int userId)
        {

            var pinnedTenantIds = await _context.PinnedItems
               .Where(pi => pi.UserId == userId && pi.ItemType == "Tenant")
               .Select(pi => (int)pi.ItemId)
               .ToListAsync();


            var pinnedTenants = await _context.Tenants
                .Where(tenant => pinnedTenantIds.Contains(tenant.Id))
                .ToListAsync();

            return pinnedTenants;
        }


        public async Task<DashBoardProjectsDTO> GetAnalysisProjectsSummaryAsync(int Projectid, int? userid, string? includeProperties = null)
        {
            var projectsQuery = _context.Projects
                .AsNoTracking()
                .Where(p => p.Id == Projectid);

            // Apply includes if any
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    projectsQuery = projectsQuery.Include(includeProp.Trim());
                }
            }

            // Get project(s) that belong to this user
            var userProjectIds = await _context.UserProjects
                .AsNoTracking()
                .Where(up => up.UserId == userid)
                .Select(up => up.ProjectId)
                .ToListAsync();

            var filteredProjects = await projectsQuery
                .Where(p => userProjectIds.Contains(p.Id))
                .ToListAsync();

            var filteredProjectIds = filteredProjects.Select(p => p.Id).ToList();

            // Issues info
            var totalIssues = await _context.Issues
                .AsNoTracking()
                .Where(i => filteredProjectIds.Contains(i.ProjectId))
                .ToListAsync();

            var completedIssues = totalIssues.Where(i => i.Status == "Completed").ToList();
            var inProgressIssues = totalIssues.Where(i => i.Status == "In Progress").ToList();
            var overdueIssues = totalIssues.Where(i => i.Status == "Postponed").ToList();
            var tenantId = await _context.Projects
               .AsNoTracking()
               .Where(p => p.Id == Projectid)
                .Select(p => p.TenantId)
                  .FirstOrDefaultAsync();

            if (tenantId == 0)
                return null; 

            var tenant = await _context.Tenants
                .AsNoTracking()
                .Include(t => t.Owner)
                .Include(t => t.JoinedUsers)
                .FirstOrDefaultAsync(t => t.Id == tenantId);


            return new DashBoardProjectsDTO
            {
                TotalIssues = totalIssues.Count,
                CompletedIssues = completedIssues.Count,
                IssuesInProgress = inProgressIssues.Count,
                IssuesOverdue = overdueIssues.Count,
                Tenant = _mapper.Map<TenantDTO>(tenant),

                
               
            };
        }

    }

}
