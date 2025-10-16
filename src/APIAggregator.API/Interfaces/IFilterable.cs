namespace APIAggregator.API.Interfaces
{
	public interface IFilterable
	{
		DateTime? CreatedAt { get; }
		string? Category { get; }
		int? Relevance { get; }
	}
}
