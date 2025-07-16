using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        [StringLength(50, ErrorMessage = "User ID must not exceed 50 characters.")]
        public int UserId { get; set; } // Target user's ID

        [Required(ErrorMessage = "Notification message is required.")]
        [StringLength(500, ErrorMessage = "Message must not exceed 500 characters.")]
        public string Message { get; set; }

        [Required(ErrorMessage = "CreatedAt is required.")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public int? IssueId;
        [ValidateNever]
        [ForeignKey(nameof(IssueId))]
        public Issue Issue { get; set; }
    }
}
