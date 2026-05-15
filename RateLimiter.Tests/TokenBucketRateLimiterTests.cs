using RateLimiter.Core.Algorithms;

namespace RateLimiter.Tests;

public class TokenBucketRateLimiterTests
{
    [Fact]
    public async Task Request_WhenBucketHasTokens_ShouldBeAllowed()
    {
        var limiter = new TokenBucketRateLimiter(capacity: 5, refillPerSecond: 1);

        var result = await limiter.IsAllowedAsync("user-1");

        Assert.True(result.IsAllowed);
        Assert.Equal(5, result.Limit);
        Assert.Equal(4, result.Remaining); // đã tiêu 1 token
    }

    [Fact]
    public async Task Request_WhenBucketEmpty_ShouldBeBlocked()
    {
        var limiter = new TokenBucketRateLimiter(capacity: 3, refillPerSecond: 1);

        // Tiêu hết 3 token
        for (int i = 0; i < 3; i++)
            await limiter.IsAllowedAsync("user-1");

        // Bucket rỗng → bị chặn
        var result = await limiter.IsAllowedAsync("user-1");

        Assert.False(result.IsAllowed);
        Assert.Equal(0, result.Remaining);
        Assert.True(result.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task Request_AfterWaiting_TokenShouldBeRefilled()
    {
        // Refill nhanh: 10 token/giây để test không phải chờ lâu
        var limiter = new TokenBucketRateLimiter(capacity: 1, refillPerSecond: 10);

        await limiter.IsAllowedAsync("user-1"); // tiêu token duy nhất

        // Chờ 200ms → refill được 2 token (nhưng capacity=1 nên chỉ có 1)
        await Task.Delay(200);

        var result = await limiter.IsAllowedAsync("user-1");

        Assert.True(result.IsAllowed); // token đã được nạp lại
    }

    [Fact]
    public async Task Request_BurstWithinCapacity_AllShouldBeAllowed()
    {
        // Token Bucket cho phép burst — gửi cùng lúc vẫn OK nếu còn token
        var limiter = new TokenBucketRateLimiter(capacity: 5, refillPerSecond: 1);

        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            var result = await limiter.IsAllowedAsync("user-1");
            results.Add(result.IsAllowed);
        }

        // Tất cả 5 request phải được cho qua
        Assert.All(results, isAllowed => Assert.True(isAllowed));
    }

    [Fact]
    public async Task DifferentClients_ShouldHaveIndependentBuckets()
    {
        var limiter = new TokenBucketRateLimiter(capacity: 1, refillPerSecond: 1);

        await limiter.IsAllowedAsync("user-1"); // user-1 hết token

        var result = await limiter.IsAllowedAsync("user-2"); // user-2 vẫn OK

        Assert.True(result.IsAllowed);
    }
}