using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [StringLength(255)]
        [Required]
        public required string Content { get; set; }

        public int? IssueId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(20)]
        [Required]
        [RegularExpression("^(Unread|Read)$", ErrorMessage = "Invalid state")]
        public required string State { get; set; }

        // Navigation Properties
        public Issue? Issue { get; set; }
    }
}
