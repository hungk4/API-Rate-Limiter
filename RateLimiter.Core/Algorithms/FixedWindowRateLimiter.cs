using System.Collections.Concurrent;
using RateLimiter.Core.Interfaces;

namespace RateLimiter.Core.Algorithms;

/// <summary>
/// Fixed Window: đếm request trong cửa sổ thời gian cố định.
/// Đơn giản, hiệu năng cao, phù hợp cho hầu hết use case.
/// </summary>
public class FixedWindowRateLimiter : IRateLimiter
{
    private readonly int _limit;           // max requests per window
    private readonly TimeSpan _window;     // kích thước cửa sổ
    private readonly ConcurrentDictionary<string, WindowEntry> _store = new();

    public FixedWindowRateLimiter(int limit, TimeSpan window)
    {
        _limit = limit;
        _window = window;
    }

    public Task<RateLimitResult> IsAllowedAsync(string clientKey)
    {
        var now = DateTime.UtcNow;

        var entry = _store.AddOrUpdate(
            key: clientKey,

            // Lần đầu gặp client này → tạo entry mới
            addValueFactory: _ => new WindowEntry(Count: 1, WindowStart: now),

            // Client đã có entry → cập nhật
            updateValueFactory: (_, existing) =>
            {
                // Cửa sổ cũ đã hết hạn → reset
                if (now - existing.WindowStart >= _window)
                    return new WindowEntry(Count: 1, WindowStart: now);

                // Còn trong cửa sổ → tăng counter
                return existing with { Count = existing.Count + 1 };
            }
        );

        var isAllowed = entry.Count <= _limit;
        var remaining = Math.Max(0, _limit - entry.Count);
        var windowEndsAt = entry.WindowStart + _window;
        var retryAfter = isAllowed ? TimeSpan.Zero : windowEndsAt - now;

        return Task.FromResult(new RateLimitResult(
            IsAllowed: isAllowed,
            Limit: _limit,
            Remaining: remaining,
            RetryAfter: retryAfter
        ));
    }

    // Record nội bộ — chỉ dùng trong class này
    private record WindowEntry(int Count, DateTime WindowStart);

}