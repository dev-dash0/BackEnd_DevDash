using System.ComponentModel.DataAnnotations;

namespace DevDash.model
{
    public class PinnedItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ItemId { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Tenant|Project|Sprint|Issue)$", ErrorMessage = "Invalid role")]
        public string ItemType { get; set; } // "Project", "Sprint", "Issue", "Tenant"

        [DataType(DataType.Date)]
        public DateTime PinnedDate { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
    }
}
