using RateLimiter.Core.Models;

namespace RateLimiter.Core.Services;

public class RateLimitConfigService
{
    private RateLimitOptions _current;
    private readonly object _lock = new();

    public RateLimitConfigService(RateLimitOptions initial)
    {
        _current = initial;
    }

    public RateLimitOptions Current
    {
        get { lock (_lock) return _current; }
    }

    public void Update(UpdateRateLimitRequest request)
    {
        lock (_lock)
        {
            // Chỉ cập nhật những field được truyền vào, giữ nguyên phần còn lại
            if (request.Enabled.HasValue)
                _current.Enabled = request.Enabled.Value;

            if (request.Limit.HasValue)
                _current.Limit = request.Limit.Value;

            if (request.WindowSeconds.HasValue)
                _current.WindowSeconds = request.WindowSeconds.Value;

            if (request.RefillPerSecond.HasValue)
                _current.RefillPerSecond = request.RefillPerSecond.Value;
        }
    }
}

public class UpdateRateLimitRequest
{
    public bool? Enabled { get; set; }
    public int? Limit { get; set; }
    public int? WindowSeconds { get; set; }
    public double? RefillPerSecond { get; set; }
}