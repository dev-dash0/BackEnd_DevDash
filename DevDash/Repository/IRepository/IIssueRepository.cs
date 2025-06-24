using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface IIssueRepository : IRepository<Issue>
    {
        Task<Issue> UpdateAsync(Issue entity);
        Task RemoveAsync(Issue issue);

    }
}
