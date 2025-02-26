namespace DevDash.DTO.DashBoard
{
    public class DashBoardDTO
    {
        public int TotalProjects { get; set; }
        public int CompletedIssues { get; set; }
        public int IssuesInProgress { get; set; }
        public int IssuesOverdue { get; set; }
    }
}
