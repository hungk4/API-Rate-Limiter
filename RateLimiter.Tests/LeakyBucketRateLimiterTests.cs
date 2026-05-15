using RateLimiter.Core.Algorithms;

namespace RateLimiter.Tests;

public class LeakyBucketRateLimiterTests
{
    [Fact]
    public async Task Request_WithinCapacity_ShouldBeAllowed()
    {
        var limiter = new LeakyBucketRateLimiter(capacity: 5, leakPerSecond: 1);

        var result = await limiter.IsAllowedAsync("user-1");

        Assert.True(result.IsAllowed);
        Assert.Equal(5, result.Limit);
    }

    [Fact]
    public async Task Request_WhenQueueFull_ShouldBeBlocked()
    {
        var limiter = new LeakyBucketRateLimiter(capacity: 3, leakPerSecond: 1);

        // Lấp đầy hàng đợi
        for (int i = 0; i < 3; i++)
            await limiter.IsAllowedAsync("user-1");

        // Request tiếp theo phải bị chặn
        var result = await limiter.IsAllowedAsync("user-1");

        Assert.False(result.IsAllowed);
        Assert.Equal(0, result.Remaining);
        Assert.True(result.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task DifferentClients_ShouldHaveIndependentQueues()
    {
        var limiter = new LeakyBucketRateLimiter(capacity: 1, leakPerSecond: 1);

        await limiter.IsAllowedAsync("user-1"); // user-1 đầy hàng đợi

        var result = await limiter.IsAllowedAsync("user-2"); // user-2 vẫn OK

        Assert.True(result.IsAllowed);
    }
}