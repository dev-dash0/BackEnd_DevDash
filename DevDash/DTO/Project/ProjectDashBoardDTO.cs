using DevDash.DTO.Tenant;
using DevDash.model;
using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Project
{
    public class ProjectDashBoardDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string ProjectCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public DateTime CreationDate { get; set; }
        [Required]
        [RegularExpression("Low|Medium|High|Critical", ErrorMessage = "Invalid priority.")]
        public string Priority { get; set; } = string.Empty;
        [Required]
        [RegularExpression("Planning|Reviewing|Working on|Completed|Canceled|Postponed")]
        public string Status { get; set; } = string.Empty;
      
    }
}
