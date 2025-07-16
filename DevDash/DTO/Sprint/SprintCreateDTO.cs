using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Sprint
{
    public class SprintCreateDTO
    {

        public string Title { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        [MaxLength(255)]
        [RegularExpression("Planned|In Progress|Completed")]
        public string Status { get; set; }
        public string? Summary { get; set; }
        //public DateTime CreatedAt { get; set; } 
    }
}
