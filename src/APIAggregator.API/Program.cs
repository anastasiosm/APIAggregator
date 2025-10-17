using APIAggregator.API.Features.Aggregation;
using APIAggregator.API.Features.ExternalAPIs;
using APIAggregator.API.Middleware;
using Microsoft.OpenApi.Models;
using APIAggregator.API.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);

// Redis configuration
var redisConnection = builder.Configuration["REDIS_CONNECTION"] ?? "localhost:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = redisConnection;
	options.InstanceName = "APIAggregator_";
});

// Add services to the container.
builder.Services.AddControllers();

// Register HTTP clients with resilience policies
builder.Services.AddResilientHttpClient<WeatherApiClient>("https://api.openweathermap.org/");
builder.Services.AddResilientHttpClient<AirQualityApiClient>("https://api.openweathermap.org/");
builder.Services.AddResilientHttpClient<IpGeolocationClient>("https://api.ipstack.com/");

// Register external API clients here!
builder.Services.AddScoped<IIpGeolocationClient, IpGeolocationClient>();
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
