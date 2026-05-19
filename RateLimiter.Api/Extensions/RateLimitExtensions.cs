using RateLimiter.Api.Middleware;
using RateLimiter.Core.Algorithms;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using RateLimiter.Core.Services;
using StackExchange.Redis;

namespace RateLimiter.Api.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Đọc config từ appsettings.json
        var options = configuration
            .GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        Console.WriteLine($"[RateLimit] Algorithm: {options.Algorithm}");

        services.AddSingleton(options);
        services.AddSingleton(new RateLimitConfigService(options));
        services.AddSingleton<ClientKeyExtractor>();

        // 2. Chọn thuật toán dựa trên config
        IRateLimiter limiter = options.Algorithm switch
        {
            "TokenBucket" => new TokenBucketRateLimiter(
                capacity: options.Limit,
                refillPerSecond: options.RefillPerSecond),

            "LeakyBucket" => new LeakyBucketRateLimiter(
                capacity: options.Limit,
                leakPerSecond: options.RefillPerSecond),

            "RedisFixedWindow" => new RedisFixedWindowRateLimiter(
                db: ConnectionMultiplexer
                        .Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379")
                        .GetDatabase(),
                limit: options.Limit,
                window: TimeSpan.FromSeconds(options.WindowSeconds)),

            "RedisTokenBucket" => new RedisTokenBucketRateLimiter(
                db: ConnectionMultiplexer
                        .Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379")
                        .GetDatabase(),
                    capacity: options.Limit,
                    refillPerSecond: options.RefillPerSecond
            ),

            "RedisLeakyBucket" => new RedisLeakyBucketRateLimiter(
                db: ConnectionMultiplexer
                        .Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379")
                        .GetDatabase(),
                capacity: options.Limit,
                leakPerSecond: options.RefillPerSecond
            ),

            _ => new FixedWindowRateLimiter(
                limit: options.Limit,
                window: TimeSpan.FromSeconds(options.WindowSeconds))
        };

        services.AddSingleton(limiter);
        services.AddSingleton(sp => new RouteRateLimiterFactory(
            options: options,
            redisConnectionString: configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitMiddleware>();
    }
}