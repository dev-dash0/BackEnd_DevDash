using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public abstract class StandardEntity
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

        public DateTime? LastUpdate { get; set; }
    }
}
