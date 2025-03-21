using DevDash.model;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository.IRepository
{
    public interface IUserProjectRepository : IJoinRepository<UserProject>
    {

        Task JoinAsync(UserProject entity, string userId);

        Task LeaveAsync(UserProject entity, string userId);

    }
}
