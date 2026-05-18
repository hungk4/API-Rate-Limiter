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


    // ← thêm 2 property này
    /// <summary>Header chứa secret key của internal service</summary>
    public string InternalServiceHeader { get; set; } = "X-Internal-Secret";

    /// <summary>Giá trị secret — chỉ internal service biết</summary>
    public string InternalServiceSecret { get; set; } = "";

    public List<RouteRateLimitOptions> Routes { get; set; } = [];
    
}


public class RouteRateLimitOptions
{
    /// <summary>Path prefix cần áp dụng — VD: "/api/upload"</summary>
    public string Path { get; set; } = "";

    /// <summary>Algorithm riêng cho route này</summary>
    public string Algorithm { get; set; } = "FixedWindow";

    /// <summary>Giới hạn riêng cho route này</summary>
    public int Limit { get; set; }

    /// <summary>Cửa sổ thời gian riêng (giây) — chỉ dùng cho FixedWindow</summary>
    public int WindowSeconds { get; set; }

    /// <summary>Token refill hoặc leak mỗi giây — dùng cho TokenBucket, LeakyBucket</summary>
    public double RefillPerSecond { get; set; } = 10;  
}