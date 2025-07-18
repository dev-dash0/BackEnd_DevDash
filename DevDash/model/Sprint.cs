using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class Sprint
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }
        public string? Description { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        [MaxLength(255)]
        [RegularExpression("Planned|In Progress|Completed")]
        public required string Status { get; set; }
        public string? Summary { get; set; }

        // Foreign Keys

        public int? CreatedById { get; set; }

        [Required]
        public required int ProjectId { get; set; }

        [ForeignKey("TenantId")]
        public int TenantId { get; set; }

        // Navigation Properties

        public  User? CreatedBy { get; set; }
        public required Project Project { get; set; }
        public Tenant Tenant { get; set; }
        public ICollection<Issue>? Issues { get; set; }
    }

}
