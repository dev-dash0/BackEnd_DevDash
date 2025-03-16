using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Issue
{
    public class IssueDashBoardDTO
    {

        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }


        public int ProjectId { get; set; }
        public string? Description { get; set; }
        public required string Status { get; set; }
        public required string Priority { get; set; }
        public string ProjectName {  get; set; }    


    }
}
