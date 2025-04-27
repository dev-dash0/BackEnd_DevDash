using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.DTO.Project
{
    public class ProjectUpdateDTO
    {
        public string Name { get; set; }
        public string? Description { get; set; }


        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        [Required]
        [RegularExpression("Low|Medium|High|Critical", ErrorMessage = "Invalid priority.")]
        public string Priority { get; set; } = string.Empty;

        [Required]
        [RegularExpression("Planning|Reviewing|Working on|Completed|Canceled|Postponed")]
        public string Status { get; set; } = string.Empty;
    }
}
