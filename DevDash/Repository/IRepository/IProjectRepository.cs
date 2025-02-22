using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project> UpdateAsync(Project entity);
        Task RemoveAsync(Project project);

    }
}
