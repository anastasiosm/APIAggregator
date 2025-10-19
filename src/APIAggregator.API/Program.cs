using APIAggregator.API.Features.Aggregation;
using APIAggregator.API.Middleware;
using Microsoft.OpenApi.Models;
using APIAggregator.API.Infrastructure.Http;
using APIAggregator.API.Features.IpGeolocation;
using APIAggregator.API.Features.Weather;
using APIAggregator.API.Features.AirQuality;
using APIAggregator.API.Interfaces;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var redisConnection = builder.Configuration.GetConnectionString("Redis")
	?? throw new InvalidOperationException("Redis connection string is not configured.");

// TODO: for debugging.. to be deleted.
Console.WriteLine($"===== REDIS CONNECTION STRING: {redisConnection} =====");

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

// Register HTTP clients with resilience policies
builder.Services.AddResilientHttpClient<WeatherApiClient>("https://api.openweathermap.org/");
builder.Services.AddResilientHttpClient<AirQualityApiClient>("https://api.openweathermap.org/");
builder.Services.AddResilientHttpClient<IpGeolocationClient>("https://api.ipstack.com/");

// Map interfaces to implementations
builder.Services.AddScoped<IIpGeolocationClient>(sp => sp.GetRequiredService<IpGeolocationClient>());
// Register ALL ILocationDataProvider implementations for IEnumerable injection
builder.Services.AddTransient<ILocationDataProvider, WeatherApiClient>();
builder.Services.AddTransient<ILocationDataProvider, AirQualityApiClient>();

// Register aggregation service
builder.Services.AddScoped<IAggregationService, AggregationService>();

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
