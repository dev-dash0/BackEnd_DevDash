namespace DevDash.Services.IService
{
    public interface ICacheService
    {
        Task SetCacheValueAsync(string key, object value, TimeSpan duration);
        Task<string>? GetCacheValueAsync(string key);
    }
}
