using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class Issue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsBacklog { get; set; } = true;
        public bool IsPinned { get; set; }=false;

        [MaxLength(255)]
        public string? Labels { get; set; }

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? Deadline { get; set; }

        public DateTime? DeliveredDate { get; set; }

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
        //[RegularExpression("BackLog|to do|In Progress|Reviewing|Completed|Canceled|Postponed")]
        [RegularExpression("BackLog|to do|In Progress|Reviewing|Completed|Canceled|Postponed")]
        public required string Status { get; set; }

        //Foreign Keys

        public int? SprintId { get; set; } // Made nullable to allow setting to null

        [Required]
        [ForeignKey("ProjectId")]
        public required int ProjectId { get; set; }
        [ForeignKey("TenantId")]
        public int TenantId { get; set; }

        [ForeignKey("UserId")]
        public  int? CreatedById { get; set; }

        public string? AttachmentPath { get; set; }

        // Navigation Properties
        public Sprint? Sprint { get; set; }
        public required Project Project { get; set; }

        public  User? CreatedBy { get; set; }
        public Tenant Tenant { get; set; }

        public ICollection<User>? AssignedUsers { get; set; }

        public ICollection<IssueAssignedUser>? IssueAssignedUsers { get; set; }

        public ICollection<Comment>? Comments { get; set; }

    }
}
