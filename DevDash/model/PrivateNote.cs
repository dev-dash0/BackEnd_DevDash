using System.ComponentModel.DataAnnotations;

namespace DevDash.model
{
    public class PrivateNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public required string Content { get; set; }

        [Required]
        public required int UserId { get; set; }

        // Navigation Property
        [Required]
        public required User User { get; set; }
    }
}
