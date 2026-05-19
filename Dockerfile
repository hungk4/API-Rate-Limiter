# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution và restore
COPY RateLimiter.sln .
COPY RateLimiter.Api/RateLimiter.Api.csproj RateLimiter.Api/
COPY RateLimiter.Core/RateLimiter.Core.csproj RateLimiter.Core/
RUN dotnet restore

# Copy toàn bộ code và build
COPY . .
RUN dotnet publish RateLimiter.Api -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RateLimiter.Api.dll"]