# API Aggregator Service (.NET 8 / ASP.NET Core)

## Overview
The APIAggregator is an ASP.NET Core Web API that consolidates data from multiple external APIs into a single endpoint.  
The implementation follows Clean Architecture principles, Strategy Pattern, and Open/Closed Principle (OCP), allowing easy addition of new providers without modifying the aggregation logic.

### Integrated APIs
- **IPStack API**: Determines city/country based on IP
- **OpenWeatherMap API**: Provides weather data
- **OpenWeatherMap Air Pollution API**: Provides air quality data

## Features
- Parallel data fetching (asynchronous execution)
- Unified JSON response model
- Easy extension with new providers via `ILocationDataProvider`
- Filtering & sorting for providers implementing IFilterable eg call: GET /api/aggregation?category=Sports&sortBy=CreatedAt&descending=true
- Resilient HttpClient configuration with Polly policies (retry, timeout, circuit breaker)
- Swagger UI for testing

## TODO: Features
- In-memory caching to reduce external calls
- API request statistics (total requests, average response time)
- Optional JWT authentication
- Optional background service for performance anomaly detection

## Quick Start

### Requirements
- .NET 8 SDK
- API keys:
  - [IPStack](https://ipstack.com/)
  - [OpenWeatherMap](https://openweathermap.org/api)

### Clone & Build
```bash
git clone https://github.com/yourusername/api-aggregator.git
cd api-aggregator
dotnet build
