using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface IIssueAssignUserRepository:IJoinRepository<IssueAssignedUser>
    {
        Task JoinAsync(IssueAssignedUser entity, string userId);
        Task LeaveAsync(IssueAssignedUser entity, string userId);

    }
}
