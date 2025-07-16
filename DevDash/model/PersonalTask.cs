using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class PersonalTask 
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? Labels { get; set; }

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        public DateTime? StartDate { get; set; }

        public DateTime? Deadline { get; set; }

        public DateTime? DeliveredDate { get; set; }

        [MaxLength(20)]
        [Required]
        [RegularExpression("Task|Reminder", ErrorMessage = "Invalid personal task type.")]
        public required string Type { get; set; }

        [MaxLength(20)]
        [RegularExpression("Low|Medium|High|Urgent", ErrorMessage = "Invalid priority.")]
        public string? Priority { get; set; }

        [MaxLength(20)]
        [RegularExpression("to do|In Progress|Reviewing|Completed|Canceled|Postponed", ErrorMessage = "Invalid task status.")]
        public string? Status { get; set; }

        //Foreign Keys

        [Required]
        public required int UserId { get; set; }

        public required User User { get; set; }
    }
}
