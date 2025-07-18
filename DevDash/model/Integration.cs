using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DevDash.model
{
    [Index(nameof(Name), IsUnique = true, Name = "IX_Integration_Name")] // Ensuring name is unique
    public class Integration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public required string Name { get; set; } // Added 'required' modifier

        [StringLength(255)]
        public required string Description { get; set; }

        [StringLength(255)]
        public string? Visual { get; set; } // Made nullable

        [StringLength(20)]
        [RegularExpression("^(Active|Inactive)$", ErrorMessage = "Invalid status")]
        public string Status { get; set; } = "Inactive"; // Default value set to "Inactive"
    }
}
