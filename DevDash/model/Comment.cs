using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("IssueId")]
        public int IssueId { get; set; }

        [Required]
        [StringLength(255)]
        public required string Content { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        [ForeignKey("CreatedBy")]
        public int? CreatedById { get; set; }

        public string? Name { get; set; }
        [ForeignKey("TenantId")]
        public int TenantId { get; set; }
        [ForeignKey("ProjectId")]
        public int ProjectId { get; set; }

        [ForeignKey("SprintId")]
        public int SprintId { get; set; }

        public Sprint Sprint { get; set; }
        public Tenant Tenant { get; set; }
        public Project Project { get; set; }
        public required Issue Issue { get; set; }

        public  User? CreatedBy { get; set; }
    }
}
