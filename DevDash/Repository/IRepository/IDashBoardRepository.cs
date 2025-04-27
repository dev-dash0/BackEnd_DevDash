using DevDash.DTO.DashBoard;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.model;
using Microsoft.AspNetCore.Mvc;

namespace DevDash.Repository.IRepository
{
    public interface IDashBoardRepository
    {
        Task<DashBoardTenantsDTO> GetAnalysisTenantsSummaryAsync(int Tenantid,int? userid, string? includeProperties = null);
        Task<DashBoardProjectsDTO> GetAnalysisProjectsSummaryAsync(int Projectid, int? userid);
        Task<List<ProjectDashBoardDTO>> GetProjectsDashboard(int userId);
        Task<List<IssueDashBoardDTO>> GetIssuesDashboard( int userId);
        Task<List<object>> GetUserIssuesTimeline(int userId);
        Task<List<Project>> GetUserPinnedProjects(int userId);
        Task<List<Issue>> GetUserPinnedIssues(int userId);
        Task<List<Sprint>> GetUserPinnedSprints(int userId);
        Task<List<Tenant>> GetUserPinnedTenants(int userId);

    }
}
