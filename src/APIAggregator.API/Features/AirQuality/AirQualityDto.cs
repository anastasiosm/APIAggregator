namespace APIAggregator.API.Features.AirQuality
{
	/// <summary>
	/// Represents air quality data, including the Air Quality Index (AQI) and particulate matter (PM) concentrations.
	/// </summary>
	/// <param name="AQI">The Air Quality Index, a numerical value representing the overall air quality.  Higher values indicate worse air
	/// quality.</param>
	/// <param name="PM25">The concentration of fine particulate matter (PM2.5) in micrograms per cubic meter (µg/m³). PM2.5 particles are
	/// smaller than 2.5 micrometers in diameter.</param>
	/// <param name="PM10">The concentration of coarse particulate matter (PM10) in micrograms per cubic meter (µg/m³). PM10 particles are
	/// smaller than 10 micrometers in diameter.</param>
	public record AirQualityDto(int AQI, double PM25, double PM10);
}
