namespace DevDash.DTO.DashBoard
{
    public class DashBoardTenantsDTO
    {
        public int? TotalProjects { get; set; }
        //public int? TotalIssues { get; set; }
        public int? CompletedProjects { get; set; }
        public int? ProjectsInProgress { get; set; }
        public int? ProjectsOverdue { get; set; }
    }
}
