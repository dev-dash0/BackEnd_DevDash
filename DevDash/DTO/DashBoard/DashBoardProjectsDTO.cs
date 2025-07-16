using DevDash.DTO.Tenant;
using DevDash.DTO.User;
using DevDash.DTO.UserProject;

namespace DevDash.DTO.DashBoard
{
    public class DashBoardProjectsDTO
    {
        //public int? TotalProjects { get; set; }
        public int? TotalIssues { get; set; }
        public int? CompletedIssues { get; set; }
        public int? IssuesInProgress { get; set; }
        public int? IssuesOverdue { get; set; }
        public TenantDTO Tenant { get; set; }
        public UserDTO Creator { get; set; }
        public ICollection<UserProjectDTO>? UserProjects { get; set; }

    }
}
