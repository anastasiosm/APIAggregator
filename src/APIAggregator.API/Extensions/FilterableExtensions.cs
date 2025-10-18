using APIAggregator.API.Features.Aggregation;
using APIAggregator.API.Interfaces;

namespace APIAggregator.API.Extensions
{
	/// <summary>
	/// Provides extension methods for filtering and sorting collections of <see cref="IFilterable"/> items.
	/// </summary>
	public static class FilterableExtensions
	{
		/// <summary>
		/// Applies filtering and sorting to all entries in an <see cref="AggregatedItemDto"/> 
		/// that contain collections of <see cref="IFilterable"/> items.
		/// </summary>
		/// <param name="aggregated">
		/// The aggregated data object containing a dictionary of data sets.
		/// </param>
		/// <param name="category">
		/// Optional category used for filtering. 
		/// If null, no category filter is applied.
		/// </param>
		public static void ApplyFilteringAndSorting(this AggregatedItemDto aggregated, string? category)
		{
			if (aggregated == null) throw new ArgumentNullException(nameof(aggregated));

			foreach (var key in aggregated.Data.Keys.ToList())
			{
				if (aggregated.Data[key] is IEnumerable<IFilterable> items)
				{
					aggregated.Data[key] = FilterAndSortItems(items, category).ToList();
				}
			}
		}

		/// <summary>
		/// Filters and sorts a collection of <see cref="IFilterable"/> items.
		/// Only items with a valid CreatedAt date are included in the results.
		/// </summary>
		/// <param name="items">The collection of filterable items to process. Cannot be null.</param>
		/// <param name="category">The category to filter by, or null to include all items.</param>
		/// <returns>
		/// The filtered and sorted collection of items with valid dates, sorted newest to oldest. 
		/// Items without a CreatedAt date are excluded. Never null.
		/// </returns>
		private static IEnumerable<IFilterable> FilterAndSortItems(IEnumerable<IFilterable> items, string? category)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			return items
				.Where(x => x.CreatedAt.HasValue)               // Exclude items without dates
				.FilterAndSort(
					filter: x => MatchesCategory(x, category),
					sortBy: x => x.CreatedAt!.Value,            // Safe: null dates already filtered
					descending: true
				);
		}


		/// <summary>
		/// Determines whether a given <see cref="IFilterable"/> item matches the specified category.
		/// </summary>
		/// <param name="item">The item to evaluate. Cannot be null.</param>
		/// <param name="category">The category to compare against. If null, all items match.</param>
		/// <returns>
		/// True if the category is null (no filtering), or if the item's category matches the specified category.
		/// </returns>
		private static bool MatchesCategory(IFilterable item, string? category)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			return category == null || item.Category == category;
		}
	}
}
