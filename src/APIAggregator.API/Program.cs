using APIAggregator.API.Features.Aggregation;
using APIAggregator.API.Features.ExternalAPIs;
using APIAggregator.API.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddHttpClient();

// Register external API clients here!
builder.Services.AddScoped<IIpGeolocationClient, IpGeolocationClient>();
builder.Services.AddScoped<IWeatherApiClient, WeatherApiClient>();
builder.Services.AddScoped<IAirQualityApiClient, AirQualityApiClient>();

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
