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

        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
        
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
                    db: multiplexer.GetDatabase(),
                    limit: route.Limit,
                    window: TimeSpan.FromSeconds(route.WindowSeconds)),

                "RedisTokenBucket" => new RedisTokenBucketRateLimiter(
                    db: multiplexer.GetDatabase(),
                    capacity: route.Limit,
                    refillPerSecond: route.RefillPerSecond),

                "RedisLeakyBucket" => new RedisLeakyBucketRateLimiter(
                    db: multiplexer.GetDatabase(),
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