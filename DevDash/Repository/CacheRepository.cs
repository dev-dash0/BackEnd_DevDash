using DevDash.Repository.IRepository;
using StackExchange.Redis;
using System.Text.Json;

namespace DevDash.Repository
{
    public class CacheRepositroy(IConnectionMultiplexer connection) : ICacheRepository
    {
        private readonly IDatabase _database = connection.GetDatabase();
        public async Task<string> GetAsync(string key)
        {
            var value = await _database.StringGetAsync(key);
            return !value.IsNullOrEmpty ? value : default;
        }

        public async Task SetAsync(string key, object value, TimeSpan duration)
        {
            var redisValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, redisValue, duration);
        }
    }
}
