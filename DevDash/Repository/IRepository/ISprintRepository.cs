using DevDash.model;
using System.Linq.Expressions;

namespace DevDash.Repository.IRepository
{
    public interface ISprintRepository:IRepository<Sprint>
    {
        Task<Sprint> UpdateAsync(Sprint sprint);
        Task RemoveAsync(Sprint sprint);
    }
}
