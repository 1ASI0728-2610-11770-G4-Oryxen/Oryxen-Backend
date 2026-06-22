# ============================================================================
# Oryxen Backend — Multi-stage Dockerfile (ASP.NET Core 9, Release mode)
# ============================================================================
# Stage 1: Build & publish using the SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY src/Oryxen.Domain/Oryxen.Domain.csproj src/Oryxen.Domain/
COPY src/Oryxen.Application/Oryxen.Application.csproj src/Oryxen.Application/
COPY src/Oryxen.Infrastructure/Oryxen.Infrastructure.csproj src/Oryxen.Infrastructure/
COPY src/Oryxen.API/Oryxen.API.csproj src/Oryxen.API/

# Restore dependencies
RUN dotnet restore "src/Oryxen.API/Oryxen.API.csproj"

# Copy the entire source tree
COPY . .

# Build in Release mode and publish to /app/out
RUN dotnet publish "src/Oryxen.API/Oryxen.API.csproj" \
    -c Release \
    -o /app/out \
    /p:UseAppHost=false

# ----------------------------------------------------------------------------
# Stage 2: Runtime image (minimal, no SDK)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published artifacts from build stage
COPY --from=build /app/out .

# Expose HTTP port (ASP.NET Core defaults to 8080 in container mode)
EXPOSE 8080

# Set environment variables for container runtime
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Define health check against the API root
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the API
ENTRYPOINT ["dotnet", "Oryxen.API.dll"]
