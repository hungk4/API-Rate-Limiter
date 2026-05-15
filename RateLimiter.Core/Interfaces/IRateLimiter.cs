namespace RateLimiter.Core.Interfaces;

public interface IRateLimiter
{
    /// <summary>
    /// Kiểm tra xem client có được phép thực hiện request không.
    /// </summary>
    /// <param name="clientKey">Định danh client (IP, userId, API key...)</param>
    /// <returns>RateLimitResult chứa trạng thái và metadata</returns>
    Task<RateLimitResult> IsAllowedAsync(string clientKey);
}

public record RateLimitResult(
    bool IsAllowed,        // true = cho qua, false = chặn
    int Limit,             // giới hạn tối đa
    int Remaining,         // còn lại bao nhiêu request
    TimeSpan RetryAfter    // client phải chờ bao lâu nếu bị chặn
);