using RateLimiter.Core.Algorithms;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using StackExchange.Redis;

namespace RateLimiter.Core.Services;


public class RouteRateLimiterFactory
{

    private readonly RateLimitOptions _options; // Config hiện tại — được đọc mỗi lần GetRateLimiter để phản ánh thay đổi realtime

    private readonly string _redisConnectionString; // Connection string tới Redis — dùng khi tạo Redis limiter cho default

    private readonly List<(string Path, IRateLimiter Limiter)> _routeLimiters; // Danh sách limiter theo từng route — khởi tạo 1 lần lúc app start

    private IRateLimiter? _cachedDefaultLimiter; // Cache default limiter — tránh tạo mới mỗi request, counter sẽ bị reset liên tục

    private string _cachedAlgorithm = ""; // Lưu algorithm đang được cache — khi algorithm thay đổi thì tạo lại limiter mới

    private readonly object _lock = new();    // Lock để đảm bảo chỉ 1 thread tạo default limiter tại 1 thời điểm

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

        // Cache lại default limiter — chỉ tạo mới khi algorithm thay đổi
        lock (_lock)
        {
            if (_cachedDefaultLimiter == null || _cachedAlgorithm != _options.Algorithm)
            {
                _cachedDefaultLimiter = CreateDefaultLimiter(_options.Algorithm);
                _cachedAlgorithm = _options.Algorithm;
            }
            return _cachedDefaultLimiter;
        }
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