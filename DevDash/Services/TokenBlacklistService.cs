using Microsoft.Extensions.Caching.Memory;

public class TokenBlacklistService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(3); // Adjust as needed

    public TokenBlacklistService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void AddTokenToBlacklist(string token)
    {
        var expirationTime = DateTime.UtcNow.Add(_tokenLifetime);
        _cache.Set(token, true, expirationTime);
    }

    public bool IsTokenBlacklisted(string token)
    {
        return _cache.TryGetValue(token, out _);
    }
}
