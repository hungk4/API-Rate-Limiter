namespace RateLimiter.Core.Models;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; } = true;

    /// <summary>"FixedWindow", "TokenBucket", "LeakyBucket"</summary>
    public string Algorithm { get; set; } = "FixedWindow";

    /// <summary>Số request tối đa (FixedWindow, LeakyBucket) hoặc capacity (TokenBucket)</summary>
    public int Limit { get; set; } = 100;

    /// <summary>Kích thước cửa sổ tính bằng giây — chỉ dùng cho FixedWindow</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Token refill hoặc leak mỗi giây — dùng cho TokenBucket, LeakyBucket</summary>
    public double RefillPerSecond { get; set; } = 10;

    /// <summary>"IP", "Header:X-Api-Key", "Claim:userId"
    /// IP: sử dụng địa chỉ IP của client
    /// Header:X-Api-Key: Api Key
    /// Claim: Theo tain khoản người dùng (yêu cầu phải có authentication)
    /// </summary>
    public string ClientKeySource { get; set; } = "IP";

    /// <summary>Các path không áp dụng rate limit</summary>
    public List<string> ExcludedPaths { get; set; } = ["/health", "/metrics"];
}