using DevDash.model;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository.IRepository
{
    public interface IUserProjectRepository : IJoinRepository<UserProject>
    {

        Task JoinAsync(UserProject entity, int userId);

        Task LeaveAsync(UserProject entity, int userId);

        Task InviteByEmailAsync(string email, int projectId, string role);

        Task AcceptInvitationAsync(string email, int userId);

    }
}
