using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface IIssueAssignUserRepository:IJoinRepository<IssueAssignedUser>
    {
        Task JoinAsync(IssueAssignedUser entity, int userId);
        Task LeaveAsync(IssueAssignedUser entity, int userId);

    }
}
