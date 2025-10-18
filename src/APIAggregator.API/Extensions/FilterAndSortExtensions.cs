namespace APIAggregator.API.Extensions
{
	/// <summary>
	/// Provides extension methods for filtering and sorting collections with a fluent API.
	/// </summary>
	public static class FilterAndSortExtensions
	{
		/// <summary>
		/// Filters and sorts a collection of items in a single operation.
		/// Both filtering and sorting are optional and applied only when corresponding delegates are provided.
		/// </summary>
		/// <typeparam name="T">The type of items in the collection.</typeparam>
		/// <param name="items">The collection to filter and sort. Cannot be null.</param>
		/// <param name="filter">
		/// Optional predicate to filter items. If null, no filtering is applied.
		/// </param>
		/// <param name="sortBy">
		/// Optional function to extract the sort key from each item. If null, no sorting is applied.
		/// </param>
		/// <param name="descending">
		/// If true, sorts in descending order; otherwise, sorts in ascending order.
		/// Only applies when <paramref name="sortBy"/> is provided.
		/// </param>
		/// <returns>
		/// The filtered and sorted collection. Never null, but may be empty if all items are filtered out.
		/// </returns>
		public static IEnumerable<T> FilterAndSort<T>(
			this IEnumerable<T> items,
			Func<T, bool>? filter = null,
			Func<T, object>? sortBy = null,
			bool descending = false
		)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));

			var result = items;

			// Apply filter if provided
			if (filter != null)
				result = result.Where(filter);

			// Apply sorting if provided
			if (sortBy != null)
				result = descending ? result.OrderByDescending(sortBy) : result.OrderBy(sortBy);

			return result;
		}
	}
}
