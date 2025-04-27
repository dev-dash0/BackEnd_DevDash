using DevDash.model;
using DevDash.Repository.IRepository;
namespace DevDash.Repository
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        private readonly AppDbContext _db;
        public CommentRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }
        public async Task RemoveAsync(Comment comment)
        {
            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
        }
        public async Task<Comment> UpdateAsync(Comment comment)
        {
            _db.Comments.Update(comment);
            await _db.SaveChangesAsync();
            return comment;
        }
    }
}
