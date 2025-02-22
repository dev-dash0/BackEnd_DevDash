using DevDash.DTO.Comment;
using DevDash.DTO.User;
using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Issue
{
    public class IssueDTO
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }
        public bool IsBacklog { get; set; } = true;
        public DateTime? CreationDate { get; set; } 
        public DateOnly? StartDate { get; set; }
        public DateOnly? Deadline { get; set; }
        public DateOnly? DeliveredDate { get; set; }
        public DateTime? LastUpdate { get; set; }

        [MaxLength(20)]
        [RegularExpression("Bug|Feature|Task|Epic", ErrorMessage = "Invalid issue type.")]
        [Required]
        public required string Type { get; set; }

        [MaxLength(20)]
        [Required]
        [RegularExpression("Low|Medium|High|Critical", ErrorMessage = "Invalid priority.")]
        public required string Priority { get; set; }

        [MaxLength(20)]
        [Required]
        [RegularExpression("BackLog|to do|In Progress|Reviewing|Completed|Canceled|Postponed")]
        public required string Status { get; set; }

        public int? CreatedById { get; set; }
        public int TenantId { get; set; }
        public int ProjectId { get; set; }
        public int? SprintId { get; set; }

        public ICollection<UserDTO>? AssignedUsers { get; set; }
        //public ICollection<CommentDTO>? Comments { get; set; }

    }
}
