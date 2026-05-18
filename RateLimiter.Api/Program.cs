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

app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello World!" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/metrics/ratelimit", (RateLimitMonitor monitor) => Results.Ok(monitor.GetStats()));

app.Run();