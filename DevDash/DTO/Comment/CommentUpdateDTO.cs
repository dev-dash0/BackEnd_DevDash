using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Comment
{
    public class CommentUpdateDTO
    {
        public int Id { get; set; }

        //[Required]
        //[ForeignKey("IssueId")]
        //public int IssueId { get; set; }
        [Required]
        [StringLength(255)]
        public required string Content { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? CreationDate { get; set; } = DateTime.Now;

        //[ForeignKey("TenantId")]
        //public int TenantId { get; set; }
        //[ForeignKey("ProjectId")]
        //public int ProjectId { get; set; }

        //[ForeignKey("SprintId")]
        //public int SprintId { get; set; }
        //[ForeignKey("CreatedBy")]
        //public int? CreatedById { get; set; }
    }
}
