using Microsoft.AspNetCore.Mvc;
using RateLimiter.Core.Services;

namespace RateLimiter.Api.Controllers;

[ApiController]
[Route("api/admin/ratelimit")]
public class RateLimitAdminController(
    RateLimitConfigService configService,
    RateLimitMonitor monitor) : ControllerBase
{
    /// <summary>Xem config hiện tại</summary>
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(configService.Current);
    }

    /// <summary>Cập nhật config — chỉ truyền field muốn thay đổi</summary>
    [HttpPatch("config")]
    public IActionResult UpdateConfig([FromBody] UpdateRateLimitRequest request)
    {
        configService.Update(request);
        return Ok(new
        {
            message = "Config updated successfully",
            current = configService.Current
        });
    }

    /// <summary>Tắt rate limit</summary>
    [HttpPost("disable")]
    public IActionResult Disable()
    {
        configService.Update(new UpdateRateLimitRequest { Enabled = false });
        return Ok(new { message = "Rate limit disabled", current = configService.Current });
    }

    /// <summary>Bật lại rate limit</summary>
    [HttpPost("enable")]
    public IActionResult Enable()
    {
        configService.Update(new UpdateRateLimitRequest { Enabled = true });
        return Ok(new { message = "Rate limit enabled", current = configService.Current });
    }

    /// <summary>Xem thống kê</summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        return Ok(monitor.GetStats());
    }

}
