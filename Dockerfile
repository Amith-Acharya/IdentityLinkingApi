# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["IdentityLinkingApi.csproj", "./"]
RUN dotnet restore "IdentityLinkingApi.csproj"

# Copy remaining files and build
COPY . .
RUN dotnet build "IdentityLinkingApi.csproj" -c Release -o /app/build
RUN dotnet publish "IdentityLinkingApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install SQLite native libraries required for EF Core on Linux
RUN apt-get update && apt-get install -y --no-install-recommends \
    libsqlite3-0 \
    libsqlite3-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy published files from build stage
COPY --from=build /app/publish .

# Set environment variables - Render provides the port via PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-5000}
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose the port (Render uses the PORT env var)
EXPOSE 5000

# Run the application
ENTRYPOINT ["dotnet", "IdentityLinkingApi.dll"]

