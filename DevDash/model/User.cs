using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Text.Json.Serialization;


namespace DevDash.model
{
    [Index(nameof(UserName), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class User : IdentityUser<int>
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public required string LastName { get; set; }

        public string Name => FirstName + " " + LastName;

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public override string UserName { get; set; }

        [Required]
        [MaxLength(255)]
        public override string Email { get; set; }

        public DateOnly? Birthday { get; set; }

        [Phone]
        [MaxLength(50)]

        [RegularExpression(@"^[0-9\+]{10,15}$")]
        public override string? PhoneNumber { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.UtcNow; // Set default value


        public DateTime? LastActiveDate { get; set; }

        public string? ImageUrl { get; set; }

        public bool Verified { get; set; } = false;

        public String? Personality { get; set; }
      
        
    
        // Navigation Properties

        public ICollection<PersonalTask>? PersonalTasks { get; set; }
        public ICollection<PrivateNote>? PrivateNotes { get; set; }
        [JsonIgnore]
        public ICollection<Tenant>? OwnedTenants { get; set; }
        public ICollection<Tenant>? JoinedTenants { get; set; }
        public ICollection<UserTenant>? UserTenants { get; set; }
        public ICollection<Project>? ManagedProjects { get; set; }
        public ICollection<Project>? CreatedProjects { get; set; }
        public ICollection<Project>? WorkingProjects { get; set; }
        public ICollection<UserProject>? UserProjects { get; set; }

        public ICollection<Sprint>? SprintsCreated { get; set; }

        public ICollection<Issue>? IssuesCreated { get; set; }

        public ICollection<Issue>? IssuesAssigned { get; set; }

        public ICollection<IssueAssignedUser>? IssueAssignedUsers { get; set; }
        public ICollection<Comment>? Comments { get; set; }
    }
}
