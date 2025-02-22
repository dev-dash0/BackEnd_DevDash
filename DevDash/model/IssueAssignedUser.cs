using System.ComponentModel.DataAnnotations.Schema;

namespace DevDash.model
{
    public class IssueAssignedUser
    {
        public int IssueId { get; set; }
        public int UserId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Assign_date { get; set; } = DateTime.Now;

        // Navigation Properties
        public required Issue Issue { get; set; }
        public required User User { get; set; }
    }
}
