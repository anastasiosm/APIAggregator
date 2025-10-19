namespace APIAggregator.API.Features.Aggregation
{
	/// <summary>
	/// DTO object that represents an aggregated data item with geographic information and associated metadata.
	/// </summary>
	/// <param name="City">The name of the city associated with the data item. Cannot be null.</param>
	/// <param name="Country">The name of the country associated with the data item. Cannot be null.</param>
	/// <param name="Latitude">The latitude of the geographic location, in decimal degrees. Must be in the range -90 to 90.</param>
	/// <param name="Longitude">The longitude of the geographic location, in decimal degrees. Must be in the range -180 to 180.</param>
	/// <param name="Data">A dictionary containing additional metadata associated with the data item.  The keys represent metadata field
	/// names, and the values represent their corresponding values.  Values may be null if the metadata field is optional
	/// or not provided.</param>
	public record AggregatedItemDto(
		string City,
		string Country,
		double Latitude,
		double Longitude,
		Dictionary<string, object?> Data);	
}
