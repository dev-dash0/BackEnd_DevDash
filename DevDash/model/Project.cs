using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        public string ProjectCode { get; set; } 

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Name { get; set; }

        public string? Description { get; set; }

        public bool IsPinned { get; set; } = false;

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        [Required]
        [RegularExpression("Planning|Reviewing|Working on|Completed|Canceled|Postponed")]
        public string Status { get; set; } = string.Empty;

        [Required]
        [RegularExpression("Low|Medium|High|Critical", ErrorMessage = "Invalid priority.")]
        public string Priority { get; set; } = string.Empty;

        // Foreign Keys
        [ForeignKey("TenantId")]
        public int TenantId { get; set; }
        public int? ProjectManagerId { get; set; }
        [ForeignKey("UserId")]
        public int CreatorId { get; set; }


        // Navigation Properties
        public User? Manager { get; set; }
        public Tenant Tenant { get; set; }
        public ICollection<User>? Users { get; set; }
        public User Creator { get; set; }
        public ICollection<UserProject>? UserProjects { get; set; }

        public ICollection<Sprint>? Sprints { get; set; }

        public ICollection<Issue>? Issues { get; set; }
    }
}
