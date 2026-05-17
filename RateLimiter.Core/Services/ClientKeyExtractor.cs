using Microsoft.AspNetCore.Http;
using RateLimiter.Core.Models;


namespace RateLimiter.Core.Services;

public class ClientKeyExtractor(RateLimitOptions options)
{   
    public string Extract(HttpContext context)
    {
        var source = options.ClientKeySource;

        if (source == "IP")
        {
            // Ưu tiên X-Forwarded-For khi đứng sau reverse proxy
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault(); // lấy proxy đầu tiên 
            if (!string.IsNullOrEmpty(forwarded))
                return forwarded.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        if(source.StartsWith("Header:"))
        {
            var headerName = source.Substring("Header:".Length);
            return context.Request.Headers[headerName].FirstOrDefault() ?? "anonymous";
        }

        if(source.StartsWith("Claim:"))
        {
            var claimType = source.Substring("Claim:".Length);
           return context.User?.FindFirst(claimType)?.Value ?? "anonymous";
        }

        return "default";
    }
}