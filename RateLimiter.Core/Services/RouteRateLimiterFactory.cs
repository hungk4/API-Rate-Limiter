using RateLimiter.Core.Algorithms;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using StackExchange.Redis;

namespace RateLimiter.Core.Services;


public class RouteRateLimiterFactory
{
    private readonly RateLimitOptions _options;
    private readonly string _redisConnectionString;
    private readonly List<(string Path, IRateLimiter Limiter)> _routeLimiters;

    public RouteRateLimiterFactory(
        RateLimitOptions options,
        string redisConnectionString)
    {
        _options = options;
        _redisConnectionString = redisConnectionString;

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
            Limiter: CreateLimiter(route.Algorithm, route, db)
        )).ToList();
    }

    public IRateLimiter GetRateLimiter(string path)
    {
        var routeLimiter = _routeLimiters
            .FirstOrDefault(r => path.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase));

        // Có route config riêng → dùng limiter của route
        if (routeLimiter.Limiter != null)
            return routeLimiter.Limiter;

        // Không có → tạo limiter theo algorithm hiện tại trong config
        return CreateDefaultLimiter(_options.Algorithm);
    }

    private IRateLimiter CreateDefaultLimiter(string algorithm)
    {
        // Tạo mới mỗi lần gọi → phản ánh đúng config hiện tại
        var needsRedis = algorithm is "RedisFixedWindow" or "RedisTokenBucket" or "RedisLeakyBucket";
        IDatabase? db = null;

        if (needsRedis)
        {
            var redisOptions = ConfigurationOptions.Parse(_redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            db = ConnectionMultiplexer.Connect(redisOptions).GetDatabase();
        }

        return CreateLimiter(algorithm, new RouteRateLimitOptions
        {
            Limit = _options.Limit,
            WindowSeconds = _options.WindowSeconds,
            RefillPerSecond = _options.RefillPerSecond
        }, db);
    }

    private static IRateLimiter CreateLimiter(string algorithm, RouteRateLimitOptions route, IDatabase? db)
    {
        return algorithm switch
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
        };
    }
}