using DevDash.DTO.DashBoard;
using DevDash.DTO.Issue;
using DevDash.DTO.Project;
using DevDash.model;
using Microsoft.AspNetCore.Mvc;

namespace DevDash.Repository.IRepository
{
    public interface IDashBoardRepository
    {
        Task<DashBoardDTO> GetAnalysisSummaryAsync(int Tenantid,int? userid);
        Task<List<ProjectDashBoardDTO>> GetProjectsDashboard(int Tenantid,int userId);
        Task<List<IssueDashBoardDTO>> GetIssuesDashboard(int tenantId, int userId);
        Task<ActionResult<Dictionary<string, object>>> GetUserIssuesTimeline(int userId);

    }
}
