using RateLimiter.Api.Middleware;
using RateLimiter.Core.Algorithms;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using RateLimiter.Core.Services;

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

        services.AddSingleton(options);
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

            _ => new FixedWindowRateLimiter(
                limit: options.Limit,
                window: TimeSpan.FromSeconds(options.WindowSeconds))
        };

        services.AddSingleton(limiter);
        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitMiddleware>();
    }
}