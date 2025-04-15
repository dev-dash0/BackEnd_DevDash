using DevDash.model;
using System.Linq.Expressions;

namespace DevDash.Repository.IRepository
{
    public interface ICommentRepository:IRepository<Comment>
    {
        Task<Comment> UpdateAsync(Comment comment);
        Task RemoveAsync(Comment comment);

      
    }

}
