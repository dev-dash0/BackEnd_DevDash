using DevDash.DTO.Comment;
using DevDash.DTO.User;
using DevDash.model;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DevDash.DTO.Issue
{
    public class IssueCreataDTO
    {

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }
        [JsonIgnore]
        public bool IsBacklog { get; set; }
        //public DateTime CreationDate { get; set; } ;
        public DateTime? StartDate { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public IFormFile? Attachment { get; set; }
        public DateTime? LastUpdate { get; set; }
        [MaxLength(255)]
        public string? Labels { get; set; }

        [MaxLength(20)]
        [RegularExpression("Bug|Feature|Task|Epic", ErrorMessage = "Invalid issue type.")]
        [Required]
        public required string Type { get; set; }

        [MaxLength(20)]
        [Required]
        [RegularExpression("Low|Medium|High|Critical", ErrorMessage = "Invalid priority.")]
        public required string Priority { get; set; }

        [MaxLength(20)]
        [Required]
        [RegularExpression("BackLog|to do|In Progress|Reviewing|Completed|Canceled|Postponed")]
        public required string Status { get; set; }

        //[BindNever]
        //public int? CreatedById { get; set; }
        //[BindNever]
        //public int TenantId { get; set; }
        //[BindNever]
        //public int ProjectId { get; set; }
        //[BindNever]
        //public int? SprintId { get; set; } = null;

    }
}
