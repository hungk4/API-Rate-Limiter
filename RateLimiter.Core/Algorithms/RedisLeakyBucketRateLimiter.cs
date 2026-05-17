using StackExchange.Redis;
using RateLimiter.Core.Interfaces;

namespace RateLimiter.Core.Algorithms;

/// <summary>
/// Leaky Bucket dùng Redis: request được xử lý với tốc độ cố định.
/// Dùng Lua Script để đảm bảo atomic — không race condition khi scale ngang.
/// </summary>
public class RedisLeakyBucketRateLimiter : IRateLimiter
{
    private readonly IDatabase _db;
    private readonly int _capacity;
    private readonly double _leakPerSecond;

    private const string Script = @"
        local key = KEYS[1]
        local capacity = tonumber(ARGV[1])
        local leakPerSecond = tonumber(ARGV[2])
        local now = tonumber(ARGV[3])

        -- Đọc trạng thái hiện tại
        local data = redis.call('HMGET', key, 'queueSize', 'lastLeakTime')
        local queueSize = tonumber(data[1])
        local lastLeakTime = tonumber(data[2])

        -- Lần đầu tiên → khởi tạo queue trống
        if queueSize == nil then
            queueSize = 0
            lastLeakTime = now
        end

        -- Tính số request đã leak kể từ lần cuối
        local elapsed = now - lastLeakTime
        local leaked = elapsed * leakPerSecond
        queueSize = math.max(0, queueSize - leaked)
        lastLeakTime = now

        -- Kiểm tra còn chỗ trong queue không
        local allowed = 0
        local remaining = 0
        local retryAfter = 0

        if math.ceil(queueSize) < capacity then
            queueSize = queueSize + 1
            allowed = 1
            remaining = math.floor(capacity - queueSize)
        else
            retryAfter = 1.0 / leakPerSecond
        end

        -- Lưu lại trạng thái mới, tự xóa sau 1 giờ không dùng
        redis.call('HMSET', key, 'queueSize', queueSize, 'lastLeakTime', lastLeakTime)
        redis.call('EXPIRE', key, 3600)

        return {allowed, remaining, retryAfter}
    ";

    public RedisLeakyBucketRateLimiter(IDatabase db, int capacity, double leakPerSecond)
    {
        _db = db;
        _capacity = capacity;
        _leakPerSecond = leakPerSecond;
    }

    public async Task<RateLimitResult> IsAllowedAsync(string clientKey)
    {
        var redisKey = $"ratelimit:leakybucket:{clientKey}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        var result = await _db.ScriptEvaluateAsync(Script,
            keys: new RedisKey[] { redisKey },
            values: new RedisValue[] { _capacity, _leakPerSecond, now }
        );

        // Lua trả về array {allowed, remaining, retryAfter}
        var results = (RedisResult[])result!;
        var allowed = (int)results[0] == 1;
        var remaining = (int)results[1];
        var retryAfter = (double)results[2];

        return new RateLimitResult(
            IsAllowed: allowed,
            Limit: _capacity,
            Remaining: remaining,
            RetryAfter: TimeSpan.FromSeconds(retryAfter)
        );
    }
}