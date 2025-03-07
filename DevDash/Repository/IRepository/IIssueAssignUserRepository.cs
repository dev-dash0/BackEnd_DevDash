using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface IIssueAssignUserRepository:IJoinRepository<IssueAssignedUser>
    {

        public Task LeaveAsync(IssueAssignedUser entity);

    }
}
