using DevDash.model;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository.IRepository
{
    public interface IUserProjectRepository : IJoinRepository<UserProject>, IRepository<UserProject>
    {

        Task JoinAsync(UserProject entity, int userId);

        Task LeaveAsync(UserProject entity, int userId);

    }
}
