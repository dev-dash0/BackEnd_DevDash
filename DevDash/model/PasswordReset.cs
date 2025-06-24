
namespace DevDash.model
{
    public class PasswordReset
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Step { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
    }
}
