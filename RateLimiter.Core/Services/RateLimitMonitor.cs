namespace RateLimiter.Core.Services;

/// <summary>
/// Theo dõi số liệu Rate Limiter trong memory.
/// Đủ dùng cho single instance — dùng Redis nếu cần share giữa nhiều instance.
/// </summary>

public class RateLimitMonitor
{
    private long _totalAllowed;
    private long _totalBlocked;

    public void RecordAllowed() => Interlocked.Increment(ref _totalAllowed);
    public void RecordBlocked() => Interlocked.Increment(ref _totalBlocked);

    public MonitorStats GetStats()
    {
        var totalRequests = _totalAllowed + _totalBlocked;
        var blockedRate = totalRequests > 0 ? (double)_totalBlocked / totalRequests * 100 : 0;

        return new MonitorStats(
            TotalAllowed: _totalAllowed,
            TotalBlocked: _totalBlocked,
            TotalRequests: totalRequests,
            BlockedRate: Math.Round(blockedRate, 2)
        );
    }

    public record MonitorStats(
        long TotalAllowed,
        long TotalBlocked,
        long TotalRequests,
        double BlockedRate  // phần trăm request bị chặn
    );
}