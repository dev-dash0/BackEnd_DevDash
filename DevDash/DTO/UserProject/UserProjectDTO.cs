using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.UserProject
{
    public class UserProjectDTO
    {
        public int UserId { get; set; }
        public int ProjectId { get; set; }

        [StringLength(20)]
        [RegularExpression("^(Admin|Developer|Project Manager)$", ErrorMessage = "Invalid role")]
        public required string Role { get; set; }

        public DateTime JoinedDate { get; set; }
        public bool AcceptedInvitation { get; set; }

    }
}
