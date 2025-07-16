namespace DevDash.Repository.IRepository
{
    public interface ICacheRepository
    {
        Task SetAsync(string key, object value, TimeSpan duration);
        Task<string> GetAsync(string key);
    }
}
