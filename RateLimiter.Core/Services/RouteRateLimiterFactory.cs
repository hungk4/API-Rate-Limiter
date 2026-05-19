using RateLimiter.Core.Algorithms;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using StackExchange.Redis;

namespace RateLimiter.Core.Services;


public class RouteRateLimiterFactory
{
    private readonly IRateLimiter _defaultLimiter;
    private readonly List<(string Path, IRateLimiter Limiter)> _routeLimiters;

    public RouteRateLimiterFactory(
    IRateLimiter defaultLimiter,
    RateLimitOptions options,
    string redisConnectionString)
    {
        _defaultLimiter = defaultLimiter;

        // Chỉ kết nối Redis khi có ít nhất 1 route dùng Redis algorithm
        var needsRedis = options.Routes.Any(r =>
            r.Algorithm is "RedisFixedWindow" or "RedisTokenBucket" or "RedisLeakyBucket");

        IDatabase? db = null;
        if (needsRedis)
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            db = ConnectionMultiplexer.Connect(redisOptions).GetDatabase();
        }

        _routeLimiters = options.Routes.Select(route => (
            Path: route.Path,
            Limiter: (IRateLimiter)(route.Algorithm switch
            {
                "TokenBucket" => new TokenBucketRateLimiter(
                    capacity: route.Limit,
                    refillPerSecond: route.RefillPerSecond),

                "LeakyBucket" => new LeakyBucketRateLimiter(
                    capacity: route.Limit,
                    leakPerSecond: route.RefillPerSecond),

                "RedisFixedWindow" => new RedisFixedWindowRateLimiter(
                    db: db!,
                    limit: route.Limit,
                    window: TimeSpan.FromSeconds(route.WindowSeconds)),

                "RedisTokenBucket" => new RedisTokenBucketRateLimiter(
                    db: db!,
                    capacity: route.Limit,
                    refillPerSecond: route.RefillPerSecond),

                "RedisLeakyBucket" => new RedisLeakyBucketRateLimiter(
                    db: db!,
                    capacity: route.Limit,
                    leakPerSecond: route.RefillPerSecond),

                _ => new FixedWindowRateLimiter(
                    limit: route.Limit,
                    window: TimeSpan.FromSeconds(route.WindowSeconds))
            })
        )).ToList();
    }

    public IRateLimiter GetRateLimiter(string path)
    {
        var routeLimiter = _routeLimiters.FirstOrDefault(r => path.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase));
        return routeLimiter.Limiter ?? _defaultLimiter;
    }
}