using RateLimiter.Core.Algorithms;

namespace RateLimiter.Tests;

public class FixedWindowRateLimiterTests
{
    [Fact]
    public async Task Request_WithinLimit_ShouldBeAllowed()
    {
        var limiter = new FixedWindowRateLimiter(limit: 5, window: TimeSpan.FromMinutes(1));

        var result = await limiter.IsAllowedAsync("user-1");

        Assert.True(result.IsAllowed);
        Assert.Equal(5, result.Limit);
        Assert.Equal(4, result.Remaining); // đã dùng 1
    }

    [Fact]
    public async Task Request_ExceedingLimit_ShouldBeBlocked()
    {
        var limiter = new FixedWindowRateLimiter(limit: 3, window: TimeSpan.FromMinutes(1));

        // Gửi 3 request hợp lệ
        for (int i = 0; i < 3; i++)
            await limiter.IsAllowedAsync("user-1");

        // Request thứ 4 phải bị chặn
        var result = await limiter.IsAllowedAsync("user-1");

        Assert.False(result.IsAllowed);
        Assert.Equal(0, result.Remaining);
        Assert.True(result.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task DifferentClients_ShouldHaveIndependentCounters()
    {
        var limiter = new FixedWindowRateLimiter(limit: 1, window: TimeSpan.FromMinutes(1));

        await limiter.IsAllowedAsync("user-1"); // user-1 hết quota

        var result = await limiter.IsAllowedAsync("user-2"); // user-2 vẫn OK

        Assert.True(result.IsAllowed);
    }
}