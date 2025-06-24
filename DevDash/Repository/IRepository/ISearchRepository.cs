namespace DevDash.Repository.IRepository
{
    public interface ISearchRepository
    {
        Task<List<dynamic>> GlobalSearchIssues(string query, int userId);
        Task<List<dynamic>> GlobalSearchProjects(string query, int userId);
        Task<List<dynamic>> GlobalSearchSprints(string query, int userId);
        Task<List<dynamic>> GlobalSearchTenants(string query, int userId);
    }
}
