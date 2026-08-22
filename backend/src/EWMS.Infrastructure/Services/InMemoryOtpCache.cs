using EWMS.Application.Common.Interfaces;

namespace EWMS.Infrastructure.Services;

/// <summary>
/// In-memory implementation of OTP cache for development and testing.
/// NOT suitable for production (single-server only, lost on restart).
/// Replace with Redis or similar distributed cache for production.
/// </summary>
public class InMemoryOtpCache : IOtpCache
{
    private readonly Dictionary<Guid, (string HashedOtp, DateTime Expiry)> _cache = new();
    private readonly object _lockObject = new object();

    public Task SetOtpAsync(Guid sessionId, string hashedOtp, int expiryMinutes)
    {
        lock (_lockObject)
        {
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
            _cache[sessionId] = (hashedOtp, expiry);
        }
        return Task.CompletedTask;
    }

    public Task<string?> GetOtpAsync(Guid sessionId)
    {
        lock (_lockObject)
        {
            if (_cache.TryGetValue(sessionId, out var entry))
            {
                // Check if expired
                if (DateTime.UtcNow > entry.Expiry)
                {
                    _cache.Remove(sessionId);
                    return Task.FromResult<string?>(null);
                }
                return Task.FromResult<string?>(entry.HashedOtp);
            }
            return Task.FromResult<string?>(null);
        }
    }

    public Task RemoveOtpAsync(Guid sessionId)
    {
        lock (_lockObject)
        {
            _cache.Remove(sessionId);
        }
        return Task.CompletedTask;
    }
}
