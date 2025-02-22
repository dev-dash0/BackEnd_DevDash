using DevDash.model;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository.IRepository
{
    public interface IUserProjectRepository : IJoinRepository<UserProject>
    {
        public  Task LeaveAsync(UserProject entity);
        
    }
}
