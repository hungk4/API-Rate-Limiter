using RateLimiter.Api.Extensions;
using RateLimiter.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddSingleton<RateLimitMonitor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiting();

app.MapGet("/api/FixedWindow",      () => Results.Ok(new { message = "FixedWindow endpoint" }));
app.MapGet("/api/TokenBucket",      () => Results.Ok(new { message = "TokenBucket endpoint" }));
app.MapGet("/api/LeakyBucket",      () => Results.Ok(new { message = "LeakyBucket endpoint" }));
app.MapGet("/api/RedisFixedWindow", () => Results.Ok(new { message = "RedisFixedWindow endpoint" }));
app.MapGet("/api/RedisTokenBucket", () => Results.Ok(new { message = "RedisTokenBucket endpoint" }));
app.MapGet("/api/RedisLeakyBucket", () => Results.Ok(new { message = "RedisLeakyBucket endpoint" }));
app.MapGet("/health",               () => Results.Ok(new { status = "healthy" }));
app.MapGet("/metrics/ratelimit",    (RateLimitMonitor monitor) => Results.Ok(monitor.GetStats()));

app.Run();