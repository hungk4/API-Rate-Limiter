using System.Collections.Concurrent;
using RateLimiter.Core.Interfaces;

namespace RateLimiter.Core.Algorithms;

/// <summary>
/// Leaky Bucket: request xếp hàng và được xử lý với tốc độ cố định.
/// Phù hợp khi backend cần tải đều, tránh spike tuyệt đối.
/// </summary>
public class LeakyBucketRateLimiter : IRateLimiter
{
    private readonly int _capacity;          // hàng đợi chứa tối đa bao nhiêu request
    private readonly double _leakPerSecond;  // xử lý bao nhiêu request mỗi giây
    private readonly ConcurrentDictionary<string, BucketEntry> _store = new();

    public LeakyBucketRateLimiter(int capacity, double leakPerSecond) {
        _capacity = capacity;
        _leakPerSecond = leakPerSecond;
    }

    public Task<RateLimitResult> IsAllowedAsync(string clientKey)
{
    var now = DateTime.UtcNow;
    var entry = _store.GetOrAdd(clientKey, _ => new BucketEntry(0, now));

    lock (entry)
    {   
        // Tính thời gian trôi qua kể từ lần leak cuối
        var elapsed = (now - entry.LastLeakTime).TotalSeconds;
        
        // Tính số request đã được xử lý (leak đi)
        var leaked = elapsed * _leakPerSecond;

        // Giảm số lượng request đang đợi xử lý trong hàng chờ, đảm bảo không âm
        entry.QueueSize = Math.Max(0, entry.QueueSize - leaked);
        
        // Cập nhật thời gian leak cuối cùng
        entry.LastLeakTime = now;

        // Nếu còn chỗ trống trong hàng chờ
        if (Math.Ceiling(entry.QueueSize) < _capacity)
        {
            entry.QueueSize += 1;
            var remaining = (int)Math.Floor(_capacity - entry.QueueSize);

            return Task.FromResult(new RateLimitResult(
                IsAllowed: true,
                Limit: _capacity,
                Remaining: remaining,
                RetryAfter: TimeSpan.Zero
            ));
        }

        // Nếu hàng chờ đầy 
        var retryAfter = 1.0 / _leakPerSecond;  

        return Task.FromResult(new RateLimitResult(
            IsAllowed: false,
            Limit: _capacity,
            Remaining: 0,
            RetryAfter: TimeSpan.FromSeconds(retryAfter)
        ));
    }
}

    private class BucketEntry(double queueSize, DateTime lastLeakTime) {
        public double QueueSize { get; set;} = queueSize;
        public DateTime LastLeakTime { get; set;} = lastLeakTime;
    }
}