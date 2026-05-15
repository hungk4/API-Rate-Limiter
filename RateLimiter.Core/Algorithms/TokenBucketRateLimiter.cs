using System.Collections.Concurrent;
using RateLimiter.Core.Interfaces;

namespace RateLimiter.Core.Algorithms;

/// <summary>
/// Token Bucket: cho phép burst ngắn hạn, tốc độ refill đều đặn.
/// Phù hợp khi bạn muốn user thoải mái trong giới hạn nhưng
/// không bị spike đột ngột làm quá tải.
/// </summary>
public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly int _capacity;           // dung lượng tối đa của bucket
    private readonly double _refillPerSecond; // token được nạp lại mỗi giây
    private readonly ConcurrentDictionary<string, BucketEntry> _store = new();

    public TokenBucketRateLimiter(int capacity, double refillPerSecond)
    {
        _capacity = capacity;
        _refillPerSecond = refillPerSecond;
    }

    public Task<RateLimitResult> IsAllowedAsync(string clientKey)
    {
        var now = DateTime.UtcNow;

        // Dùng lock per-key thay vì global lock để giảm contention
        var entry = _store.GetOrAdd(clientKey, _ => new BucketEntry(_capacity, now));

        lock (entry)
        {
            // Tính số token được refill kể từ lần cuối
            var elapsed = (now - entry.LastRefill).TotalSeconds;
            var refilled = elapsed * _refillPerSecond;

            // Nạp token vào bucket, không vượt quá capacity
            entry.Tokens = Math.Min(_capacity, entry.Tokens + refilled);
            entry.LastRefill = now;

            if (entry.Tokens >= 1)
            {
                entry.Tokens -= 1; // tiêu 1 token
                var remaining = (int)Math.Floor(entry.Tokens);

                return Task.FromResult(new RateLimitResult(
                    IsAllowed: true,
                    Limit: _capacity,
                    Remaining: remaining,
                    RetryAfter: TimeSpan.Zero
                ));
            }

            // Hết token → tính thời gian chờ để có 1 token tiếp theo
            var waitSeconds = (1 - entry.Tokens) / _refillPerSecond;

            return Task.FromResult(new RateLimitResult(
                IsAllowed: false,
                Limit: _capacity,
                Remaining: 0,
                RetryAfter: TimeSpan.FromSeconds(waitSeconds)
            ));
        }
    }

    // Class thay vì record vì chúng ta cần mutate (thay đổi Tokens, LastRefill)
    private class BucketEntry(double tokens, DateTime lastRefill)
    {
        public double Tokens { get; set; } = tokens;
        public DateTime LastRefill { get; set; } = lastRefill;
    }
}