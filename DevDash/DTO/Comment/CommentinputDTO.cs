using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Comment
{
    public class CommentinputDTO
    {
        [Required]
        [StringLength(255)]
        public required string Content { get; set; }
    }
}
