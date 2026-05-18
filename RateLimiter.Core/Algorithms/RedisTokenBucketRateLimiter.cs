using StackExchange.Redis;
using RateLimiter.Core.Interfaces;

namespace RateLimiter.Core.Algorithms;

/// <summary>
/// Token Bucket dùng Redis: cho phép burst ngắn hạn, tốc độ refill đều đặn.
/// Dùng Lua Script để đảm bảo atomic — không race condition khi scale ngang.
/// </summary>

public class RedisTokenBucketRateLimiter : IRateLimiter
{
    private readonly IDatabase _db; 
    private readonly int _capacity;     // dung lượng tối đa của bucket
    private readonly double _refillPerSecond;   // token được nạp lại mỗi giây


    // Lua Script chạy atomic trên Redis
    private const string Script = @"
        local key = KEYS[1]
        local capacity = tonumber(ARGV[1])
        local refillPerSecond = tonumber(ARGV[2])
        local now = tonumber(ARGV[3])

        -- Đọc trạng thái hiện tại của bucket
        local data = redis.call('HMGET', key, 'tokens', 'lastRefill')
        local tokens = tonumber(data[1])
        local lastRefill = tonumber(data[2])

        -- Lần đầu tiên → khởi tạo bucket đầy
        if tokens == nil then
            tokens = capacity
            lastRefill = now
        end

        -- Tính số token được refill kể từ lần cuối
        local elapsed = now - lastRefill
        local refilled = elapsed * refillPerSecond
        tokens = math.min(capacity, tokens + refilled)
        lastRefill = now

        -- Kiểm tra còn token không
        local allowed = 0
        local remaining = 0
        local retryAfter = 0

        if tokens >= 1 then
            tokens = tokens - 1
            allowed = 1
            remaining = math.floor(tokens)
        else
            -- Tính thời gian chờ để có 1 token tiếp theo
            retryAfter = (1 - tokens) / refillPerSecond
        end

        -- Lưu lại trạng thái mới, tự xóa sau 1 giờ không dùng
        redis.call('HMSET', key, 'tokens', tokens, 'lastRefill', lastRefill)
        redis.call('EXPIRE', key, 3600)

        return {allowed, remaining, retryAfter}
    ";


    public RedisTokenBucketRateLimiter(IDatabase db, int capacity, double refillPerSecond)
    {
        _db = db;
        _capacity = capacity;
        _refillPerSecond = refillPerSecond;
    }

    public async Task<RateLimitResult> IsAllowedAsync(string clientKey)
    {
        var redisKey = $"ratelimit:tokenbucket:{clientKey}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        var result = await _db.ScriptEvaluateAsync(Script,
            keys: new RedisKey[] { redisKey },
            values: new RedisValue[] { _capacity, _refillPerSecond, now }
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