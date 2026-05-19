using System.Net;
using System.Text.Json;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using RateLimiter.Core.Services;

namespace RateLimiter.Api.Middleware;

public class RateLimitMiddleware(
    RequestDelegate next,
    RouteRateLimiterFactory factory,
    ClientKeyExtractor keyExtractor,
    RateLimitConfigService configService,
    RateLimitMonitor monitor,
    ILogger<RateLimitMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Lấy config mới nhất mỗi request — phản ánh thay đổi realtime
        var options = configService.Current;

        // 1. Tắt rate limit theo config
        if (!options.Enabled)
        {
            await next(context);
            return;
        }


        // 2. Bypass các path loại trừ
        string path = context.Request.Path.Value ?? "";
        if (options.ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (IsInternalService(context))
        {
            logger.LogInformation(
                "Internal service request bypassed. Path: {Path}", path);
            await next(context);
            return;
        }

        // 3. Lấy key định danh client
        string clientKey = keyExtractor.Extract(context);


        var rateLimiter = factory.GetRateLimiter(path); // Lấy limiter theo route, fallback về default

        // 4. Kiểm tra rate limit
        var result = await rateLimiter.IsAllowedAsync(clientKey);


        // 5. Gắn headers — dù allowed hay blocked
        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        context.Response.Headers["X-Client-Key"] = clientKey;

        if (result.IsAllowed)
        {
            monitor.RecordAllowed();

            logger.LogInformation(
            "Request allowed. Client: {ClientKey}, Path: {Path}, Remaining: {Remaining}/{Limit}",
            clientKey,
            path,
            result.Remaining,
            result.Limit);


            await next(context);
            return;
        }

        // 6. Bị chặn → 429
        monitor.RecordBlocked();

        logger.LogWarning(
            "Rate limit exceeded. Client: {ClientKey}, Path: {Path}, RetryAfter: {RetryAfter}s",
            clientKey,
            path,
            (int)result.RetryAfter.TotalSeconds);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.TotalSeconds).ToString();
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = 429,
            error = "Too Many Requests",
            message = $"Rate limit exceeded. Retry after {(int)result.RetryAfter.TotalSeconds} seconds.",
            retryAfterSeconds = (int)result.RetryAfter.TotalSeconds
        }));
    }

    public bool IsInternalService(HttpContext context)
    {
        var options = configService.Current;

        // Không config secret
        if (string.IsNullOrEmpty(options.InternalServiceSecret))
            return false;

        var headerValue = context.Request.Headers[options.InternalServiceHeader].FirstOrDefault();

        return headerValue == options.InternalServiceSecret;
    }
}