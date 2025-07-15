using DevDash.Repository.IRepository;
using DevDash.Services.IService;

namespace DevDash.Services
{
    public class CacheService(ICacheRepository cacheRepository) : ICacheService
    {
        public async Task<string>? GetCacheValueAsync(string key)
        {
            var value = await cacheRepository.GetAsync(key);
            return value == null ? null : value;
        }

        public async Task SetCacheValueAsync(string key, object value, TimeSpan duration)
        {
            await cacheRepository.SetAsync(key, value, duration);

        }
    }
}