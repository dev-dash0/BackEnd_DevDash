using AutoMapper;
using DevDash.DTO.DashBoard;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.DTO.Sprint;
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
        public async Task<DashBoardDTO> GetAnalysisSummaryAsync(int tenantId, int? userId)
        {
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
                .ToListAsync();
            var totalProjects = filteredProjects.Count();
            var filteredProjectIds = await projectsQuery
            .Where(p => userProjectIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

            var completed = await _context.Issues
            .AsNoTracking()
            .Where(i => filteredProjectIds.Contains(i.ProjectId) && i.Status == "Completed")
            .ToListAsync();

            var InProgress = await _context.Issues
            .AsNoTracking()
            .Where(i => filteredProjectIds.Contains(i.ProjectId) && i.Status == "In Progress")
             .ToListAsync();
            var Overdue = await _context.Issues
            .AsNoTracking()
           .Where(i => filteredProjectIds.Contains(i.ProjectId) && i.Status == "Postponed")
           .ToListAsync();

            var completedIssues = completed.Count();
            var issuesInProgress = InProgress.Count();
            var issuesOverdue = Overdue.Count();



            return new DashBoardDTO
            {
                TotalProjects = totalProjects,
                CompletedIssues = completedIssues,
                IssuesInProgress = issuesInProgress,
                IssuesOverdue = issuesOverdue
            };
        }
        public async Task<List<ProjectDashBoardDTO>> GetProjectsDashboard(int tenantId, int userId)
        {
            if (tenantId <= 0 || userId <= 0)
            {
                throw new ArgumentException("Tenant ID and User ID must be greater than zero.");
            }
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
                .Where(p => p.TenantId == tenantId && userProjectIds.Contains(p.Id))
                .ToListAsync();
            var projects = _mapper.Map<List<ProjectDashBoardDTO>>(filteredProjects);
            return projects;
        }




        public async Task<List<IssueDashBoardDTO>> GetIssuesDashboard(int tenantId, int userId)
        {
            if (tenantId <= 0 || userId <= 0)
            {
                throw new ArgumentException("Tenant ID and User ID must be greater than zero.");
            }
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
          .Where(issue => issue.TenantId == tenantId && userIssueIds.Contains(issue.Id))
          .Select(issue => new IssueDashBoardDTO
          {
              Id = issue.Id,
              Title = issue.Title,
              Description = issue.Description,
              Status = issue.Status,
              Priority = issue.Priority,
              ProjectId = issue.ProjectId,
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
  
        public async Task<ActionResult<Dictionary<string, object>>> GetUserIssuesTimeline(int userId)
        {
            var minStartDate = await _context.Issues
                .Where(i => i.IssueAssignedUsers.Any(ia => ia.UserId == userId))
                .Select(i => i.StartDate)
                .MinAsync();

            var maxDeadline = await _context.Issues
                .Where(i => i.IssueAssignedUsers.Any(ia => ia.UserId == userId))
                .Select(i => i.Deadline)
                .MaxAsync();
            if(minStartDate==null || maxDeadline==null)
            {
                return null;
            }


            DateTime currentDate = DateTime.UtcNow.Date;

            var issues = await _context.Issues
                .AsNoTracking()
                .Where(issue => issue.IssueAssignedUsers.Any(ia => ia.UserId == userId))
                .Include(issue => issue.Project)
                .Include(issue => issue.Tenant)
                .Include(issue => issue.AssignedUsers)
                .OrderByDescending(issue => issue.Priority)
                .ToListAsync();

            var timeline = new Dictionary<string, object>();

            for (DateTime date = minStartDate.Value; date <= maxDeadline.Value; date = date.AddDays(1))
            {
                var issuesForDay = issues
                    .Where(issue => issue.StartDate <= date && issue.Deadline >= date)
                    .Select(issue => new
                    {
                        issue.Id,
                        issue.Title,
                        issue.Priority,
                        issue.Type,
                        ProjectName = issue.Project.Name,
                        TenantName = issue.Tenant.Name,
                        StartDate = issue.StartDate,
                        Deadline = issue.Deadline
                    })
                    .ToList();

                timeline[date.ToString("yyyy-MM-dd")] = new
                {
                    currentDate = currentDate.ToString("yyyy-MM-dd"),
                    issues = issuesForDay
                };
            }

            return timeline; // سيتم تحويله تلقائيًا إلى JSON
        }



        ////
        //////////////////////////////////







    }

}