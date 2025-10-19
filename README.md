# API Aggregator Service (.NET 8 / ASP.NET Core)

## Overview
The APIAggregator is an ASP.NET Core Web API that consolidates data from multiple external APIs into a single endpoint.  
The implementation follows Clean Architecture principles, Strategy Pattern, and Open/Closed Principle (OCP), allowing easy addition of new providers without modifying the aggregation logic.

### Integrated APIs
- **IPStack API**: Determines city/country based on IP geolocation
- **OpenWeatherMap API**: Provides current weather data
- **OpenWeatherMap Air Pollution API**: Provides air quality index and pollutant data

## Features
- ✅ Parallel data fetching (asynchronous execution)
- ✅ Unified JSON response model
- ✅ Easy extension with new providers via `ILocationDataProvider`
- ✅ Filtering & sorting for providers implementing `IFilterable`
- ✅ Resilient HttpClient configuration with Polly policies (retry, timeout, circuit breaker)
- ✅ Redis distributed caching to reduce external API calls
- ✅ Docker Compose for production-like deployment
- ✅ Swagger UI for API testing and documentation
- ✅ Error handling middleware for consistent error responses

## TODO: Features
- API request statistics (total requests, average response time)
- Optional JWT authentication
- Optional background service for performance anomaly detection

---

## API Endpoints

### Get Aggregated Data
Retrieves aggregated location-based data from multiple external APIs.

**Endpoint:** `GET /api/aggregation`

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `ip` | string | No | Auto-detect | IP address to geolocate. If not provided, attempts to detect from request. |
| `category` | string | No | null | Filter results by category (for providers implementing IFilterable) |
| `sortBy` | string | No | null | Field to sort by (for providers implementing IFilterable) |
| `descending` | boolean | No | false | Sort order (true = descending, false = ascending) |

**Example Requests:**
```bash
# Get data for specific IP
GET /api/aggregation?ip=8.8.8.8

# Auto-detect IP (uses fallback in development)
GET /api/aggregation

# With filtering and sorting
GET /api/aggregation?ip=1.1.1.1&category=Sports&sortBy=CreatedAt&descending=true
```

**Response Format (200 OK):**
```json
{
  "city": "Mountain View",
  "country": "United States",
  "latitude": 37.386,
  "longitude": -122.084,
  "data": {
    "Weather": {
      "temperature": 18.5,
      "description": "Clear sky",
      "humidity": 65,
      "pressure": 1013
    },
    "AirQuality": {
      "aqi": 2,
      "co": 201.94,
      "no2": 0.45,
      "o3": 68.66,
      "pm2_5": 0.5,
      "pm10": 0.59
    }
  }
}
```

**Error Responses:**
- `400 Bad Request` - Invalid IP address or unable to determine IP
- `500 Internal Server Error` - External API failure or server error

---

## Setup & Configuration

### Requirements
- .NET 8 SDK
- Docker & Docker Compose (for containerized deployment)
- Redis (handled by Docker Compose)
- API keys:
  - [IPStack](https://ipstack.com/) - Free tier: 100 requests/month
  - [OpenWeatherMap](https://openweathermap.org/api) - Free tier: 1000 requests/day

### 1. Clone & Install Dependencies
```bash
git clone https://github.com/yourusername/api-aggregator.git
cd api-aggregator/src
dotnet restore
```

### 2. Configure API Keys

Create `appsettings.json` (or edit existing):
```json
{
  "ExternalAPIs": {
    "IPStack": {
      "BaseUrl": "https://api.ipstack.com/",
      "ApiKey": "YOUR_IPSTACK_API_KEY"
    },
    "OpenWeatherMap": {
      "BaseUrl": "https://api.openweathermap.org/data/2.5/",
      "ApiKey": "YOUR_OPENWEATHERMAP_API_KEY"
    }
  },
  "ConnectionStrings": {
    "Redis": "redis:6379"
  }
}
```

For **local development** (F5 debugging), create `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### 3. Environment Variables (Optional)

You can override settings using environment variables:
```bash
# Redis connection
export REDIS_CONNECTION="localhost:6379"

# API Keys
export ExternalAPIs__IPStack__ApiKey="your_key"
export ExternalAPIs__OpenWeatherMap__ApiKey="your_key"
```

---

## Running the Application

### Option 1: Docker Compose (Recommended for Production Testing)

**Start all services** (API, Redis, Redis Commander):
```bash
cd src
docker-compose up -d --build
```

**Access the application:**
- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Redis Commander: http://localhost:8082

**View logs:**
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f redis
```

**Stop services:**
```bash
docker-compose down

# Remove volumes (clears Redis data)
docker-compose down -v
```

---

### Option 2: Local Development (F5 Debugging)

For debugging with Visual Studio or VS Code:

**1. Start only Redis and Redis Commander:**
```bash
cd src
docker-compose up -d redis redis-commander
```

**2. Verify Redis is running:**
```bash
docker ps | findstr redis
```

**3. Run the API locally:**
```bash
# CLI
dotnet run --project APIAggregator.API

# Or press F5 in Visual Studio
```

**4. Access the application:**
- API: https://localhost:7001 or http://localhost:5000
- Swagger UI: https://localhost:7001/swagger
- Redis Commander: http://localhost:8082

---



## Docker Configuration

### docker-compose.yml Structure

```yaml
services:
  api:
    # .NET 8 Web API
    # Depends on Redis for caching
    
  redis:
    # Redis server for distributed caching
    # Exposed on port 6379
    # Data persisted in volume
    
  redis-commander:
    # Redis management UI
    # Access at http://localhost:8082
```

### Environment Variables in Docker

The `docker-compose.yml` sets these environment variables:
- `ASPNETCORE_ENVIRONMENT=Development`
- `REDIS_CONNECTION=redis:6379`

---

## Redis Caching

### Cache Configuration

- **Cache Key Format**: `APIAggregator_Aggregated:{ip}`
- **Expiration**: 5 minutes (sliding)
- **Behavior**: 
  - First request: Fetches from external APIs and caches result
  - Subsequent requests (within 5 min): Returns cached data
  - After expiration: Fetches fresh data and updates cache

### Viewing Cached Data

1. Open Redis Commander: http://localhost:8082
2. Click on "Add Redis Database" (first time only):
   - Host: `redis` (or `localhost` if running locally)
   - Port: `6379`
   - Database Alias: `APIAggregator`
3. Browse keys starting with `APIAggregator_Aggregated:`
4. Click on a key to view the cached JSON data

### Cache Management

**Clear specific cache entry:**
```bash
# Via Redis Commander UI - click Delete button

# Via Redis CLI
docker exec -it redis redis-cli
> DEL APIAggregator_Aggregated:8.8.8.8
```

**Clear all cache:**
```bash
docker exec -it redis redis-cli FLUSHALL
```

---

## Testing the API

### Using Swagger UI
1. Navigate to http://localhost:8080/swagger
2. Expand the `/api/aggregation` endpoint
3. Click "Try it out"
4. Enter parameters (optional)
5. Click "Execute"

### Using cURL
```bash
# Basic request
curl http://localhost:8080/api/aggregation?ip=8.8.8.8

# With filters
curl "http://localhost:8080/api/aggregation?ip=1.1.1.1&category=Sports&descending=true"
```

### Using PowerShell
```powershell
# Basic request
Invoke-RestMethod -Uri "http://localhost:8080/api/aggregation?ip=8.8.8.8"

# Measure response time
Measure-Command { Invoke-RestMethod -Uri "http://localhost:8080/api/aggregation?ip=8.8.8.8" }
```

### Performance Testing (Cache vs No Cache)

```powershell
# First request (no cache) - slower
Measure-Command { Invoke-RestMethod -Uri "http://localhost:8080/api/aggregation?ip=1.1.1.1" }

# Second request (cached) - much faster
Measure-Command { Invoke-RestMethod -Uri "http://localhost:8080/api/aggregation?ip=1.1.1.1" }
```

Expected results:
- First request: ~1-2 seconds (external API calls)
- Cached request: ~50-200ms (from Redis)

---

## Architecture

### Project Structure
```
APIAggregator/
├── src/
│   ├── APIAggregator.API/
│   │   ├── Extensions/               # Helper extensions (filtering, sorting)
│   │   ├── Features/
│   │   │   ├── Aggregation/          # Main aggregation logic
│   │   │   ├── AirQuality/           # Air Quality API client implementation
│   │   │   ├── IpGeolocation/        # IP Geolocation API client implementation
│   │   │   └── Weather/              # Weather API client implementation
│   │   ├── Infrastructure/           # Redis cache, HTTP configuration
│   │   ├── Interfaces/               # Abstraction layer
│   │   ├── Middleware/               # Error handling
│   │   ├── appsettings.json          # Production config
│   │   ├── appsettings.Development.json  # Development config
│   │   ├── Dockerfile
│   │   └── Program.cs                # App configuration & DI
│   ├── api-aggregator.sln
│   └── docker-compose.yml            # Container orchestration
└── README.md
```

### Key Design Patterns

**Strategy Pattern**: Each external API implements `ILocationDataProvider`
```csharp
public interface ILocationDataProvider
{
    string Name { get; }
    Task<object> GetDataAsync(double lat, double lon, CancellationToken ct);
}
```

**Open/Closed Principle**: Add new providers without modifying `AggregationService`

**Repository Pattern**: Services injected via dependency injection

**Resilience Patterns**: Polly policies for retry, timeout, and circuit breaker

---

## Troubleshooting

### Redis Connection Issues

**Symptom**: `TimeoutException: It was not possible to connect to the redis server(s)`

**Solutions:**
1. Verify Redis is running: `docker ps | findstr redis`
2. Check connection string matches environment:
   - Docker: `redis:6379`
   - Local: `localhost:6379`
3. Restart Redis: `docker restart redis`
4. Check Docker networks: `docker network inspect src_default`

### API Key Issues

**Symptom**: `401 Unauthorized` or `403 Forbidden` from external APIs

**Solutions:**
1. Verify API keys in `appsettings.json`
2. Check API quota limits (IPStack: 100/month, OpenWeatherMap: 1000/day)
3. Test keys directly: `curl "https://api.ipstack.com/8.8.8.8?access_key=YOUR_KEY"`

### Port Conflicts

**Symptom**: `Address already in use` when starting containers

**Solutions:**
```bash
# Find process using port
netstat -ano | findstr :8080

# Change port in docker-compose.yml
ports:
  - "8090:8080"  # Changed from 8080 to 8090
```

### Docker Build Issues

**Symptom**: Build fails or takes too long

**Solutions:**
```bash
# Clean Docker cache
docker builder prune

# Rebuild without cache
docker-compose build --no-cache

# Check Docker resources (memory, CPU)
docker system df
```

---

## Contributing

### Adding a New Provider

1. Create a new class implementing `ILocationDataProvider`:
```csharp
public class MyNewProvider : ILocationDataProvider
{
    public string Name => "MyProvider";
    
    public async Task<object> GetDataAsync(double lat, double lon, CancellationToken ct)
    {
        // Implementation
    }
}
```

2. Register in `Program.cs`:
```csharp
builder.Services.AddScoped<ILocationDataProvider, MyNewProvider>();
```

3. The aggregation service automatically discovers and calls it!
