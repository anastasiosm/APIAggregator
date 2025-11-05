using APIAggregator.API.Features.Aggregation;
using APIAggregator.API.Features.AirQuality;
using APIAggregator.API.Features.IpGeolocation;
using APIAggregator.API.Features.Statistics;
using APIAggregator.API.Features.Weather;
using APIAggregator.API.Infrastructure.Caching;
using APIAggregator.API.Infrastructure.Http;
using APIAggregator.API.Interfaces;
using APIAggregator.API.Middleware;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var redisConnection = builder.Configuration.GetConnectionString("Redis")
	?? throw new InvalidOperationException("Redis connection string is not configured.");

builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = redisConnection;
	options.InstanceName = "APIAggregator_";
});

// Added logging for Redis operations
builder.Logging.AddFilter("Microsoft.Extensions.Caching", LogLevel.Debug);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddOptions<WeatherApiOptions>()
	.Configure(options =>
	{
		options.ApiKey = builder.Configuration["ExternalAPIs:OpenWeatherMap:ApiKey"]
			?? throw new InvalidOperationException("OpenWeatherMap API key missing.");
		options.BaseUrl = builder.Configuration["ExternalAPIs:OpenWeatherMap:BaseUrl"]
			?? "https://api.openweathermap.org/data/2.5/";
	})
	.ValidateOnStart(); // Validates immediately on startup

builder.Services.AddOptions<AirQualityApiOptions>()
	.Configure(options =>
	{
		options.ApiKey = builder.Configuration["ExternalAPIs:OpenWeatherMap:ApiKey"]
			?? throw new InvalidOperationException("OpenWeatherMap API key missing for Air Quality.");
		options.BaseUrl = builder.Configuration["ExternalAPIs:OpenWeatherMap:BaseUrl"]
			?? "https://api.openweathermap.org/data/2.5/";
	})
	.ValidateOnStart(); // Validates immediately on startup

// Register HTTP clients with resilience policies with STATISTICS TRACKING
builder.Services.AddResilientHttpClientWithStats<WeatherApiClient>("https://api.openweathermap.org/", "Weather");
builder.Services.AddResilientHttpClientWithStats<AirQualityApiClient>("https://api.openweathermap.org/", "AirQuality");
builder.Services.AddResilientHttpClientWithStats<IpGeolocationClient>("https://api.ipstack.com/", "IpStack");

// Map interfaces to implementations - factory pattern
builder.Services.AddScoped<IIpGeolocationClient>(sp => sp.GetRequiredService<IpGeolocationClient>());
builder.Services.AddTransient<ILocationDataProvider>(sp => sp.GetRequiredService<WeatherApiClient>());
builder.Services.AddTransient<ILocationDataProvider>(sp => sp.GetRequiredService<AirQualityApiClient>());

// STATISTICS TRACKING : singleton service to keep metrics in memory
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();

// Register aggregation service with caching decorator using Scrutor
builder.Services.AddScoped<IDistributedCacheService, DistributedCacheService>();
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.Decorate<IAggregationService, CachedAggregationService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "APIAggregator", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Configure the HTTP request pipeline.

app.Run();
