using System.ComponentModel.DataAnnotations;

namespace DevDash.model
{
    public class UserProject
    {

        public int UserId { get; set; }
        public int ProjectId { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Admin|Developer|Project Manager)$", ErrorMessage = "Invalid role")]
        public required string Role { get; set; }
        public bool AcceptedInvitation { get; set; } = false;


        [DataType(DataType.Date)]
        public DateTime JoinedDate { get; set; } = DateTime.Now;

        //navigation properties
        public User User { get; set; }
        public Project Project { get; set; }

    }
}
