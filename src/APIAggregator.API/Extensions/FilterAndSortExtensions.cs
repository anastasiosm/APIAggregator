namespace APIAggregator.API.Extensions
{
	public static class FilterAndSortExtensions
	{
		public static IEnumerable<T> FilterAndSort<T>(
			this IEnumerable<T> items,
			Func<T, bool>? filter = null,
			Func<T, object>? sortBy = null,
			bool descending = false)
		{
			var result = items;
			if (filter != null)
				result = result.Where(filter);

			if (sortBy != null)
				result = descending ? result.OrderByDescending(sortBy) : result.OrderBy(sortBy);

			return result;
		}
	}
}
