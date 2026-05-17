using StackExchange.Redis;
using RateLimiter.Core.Interfaces;

namespace RateLimiter.Core.Algorithms;

/// <summary>
/// Fixed Window dùng Redis: đếm request trong cửa sổ thời gian cố định.
/// Khác FixedWindowRateLimiter ở chỗ: counter lưu trên Redis thay vì memory
/// → nhiều instance cùng chia sẻ chung counter 
/// </summary>
public class RedisFixedWindowRateLimiter : IRateLimiter
{
    private readonly IDatabase _db;        // kết nối Redis
    private readonly int _limit;           // max requests per window
    private readonly TimeSpan _window;     // kích thước cửa sổ

    public RedisFixedWindowRateLimiter(IDatabase db, int limit, TimeSpan window)
    {
        _db = db;
        _limit = limit;
        _window = window;
    }

    public async Task<RateLimitResult> IsAllowedAsync(string clientKey)
    {
        // Key trên Redis — mỗi client có key riêng
        var redisKey = $"ratelimit:fixedwindow:{clientKey}";

        // Tăng counter lên 1, Redis tự tạo key nếu chưa có
        var count = await _db.StringIncrementAsync(redisKey);

        // Lần đầu tiên (count = 1) → set thời gian hết hạn cho key
        // Redis tự xóa key sau _window giây → counter tự reset
        if (count == 1)
            await _db.KeyExpireAsync(redisKey, _window);

        var isAllowed = count <= _limit;
        var remaining = (int)Math.Max(0, _limit - count);

        // Lấy TTL còn lại để tính thời gian client phải chờ
        var ttl = await _db.KeyTimeToLiveAsync(redisKey);
        var retryAfter = isAllowed ? TimeSpan.Zero : ttl ?? _window;

        return new RateLimitResult(
            IsAllowed: isAllowed,
            Limit: _limit,
            Remaining: remaining,
            RetryAfter: retryAfter
        );
    }
}