using RateLimiter.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiting(builder.Configuration);

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

app.Run();