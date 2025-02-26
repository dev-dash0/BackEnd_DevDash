using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DevDash.DTO.Comment
{
    public class CommentUpdateDTO
    {
        public int Id { get; set; }

 
        [Required]
        [StringLength(255)]
        public required string Content { get; set; }

    
    }
}
