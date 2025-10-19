namespace APIAggregator.API.Features.ExternalAPIs
{
	/// <summary>
	/// DTO object that represents weather data, including a summary, temperature, and description.
	/// </summary>
	/// <param name="Summary">A brief summary of the weather conditions, such as "Sunny" or "Cloudy".</param>
	/// <param name="TemperatureC">The temperature in degrees Celsius.</param>
	/// <param name="Description">A detailed description of the weather conditions.</param>
	public record WeatherDto(string Summary, double TemperatureC, string Description);
}
