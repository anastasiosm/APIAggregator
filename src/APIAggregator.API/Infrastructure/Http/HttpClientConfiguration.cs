namespace APIAggregator.API.Infrastructure.Http
{
	using APIAggregator.API.Features.Statistics;
	using Microsoft.Extensions.DependencyInjection;
	using Polly;
	using Polly.Extensions.Http;
	using System.Net.Http.Headers;

	/// <summary>
	/// HTTP SETUP
	/// Provides centralized configuration for resilient <see cref="HttpClient"/> instances.
	/// Registers common policies (retry, timeout, circuit breaker) using Polly and 
	/// allows consistent setup of external API clients through dependency injection.
	/// </summary>
	public static class HttpClientConfiguration
	{
		public static void AddResilientHttpClient<TClient>(
			this IServiceCollection services,
			string baseUrl)
			where TClient : class
		{
			services.AddHttpClient<TClient>(client =>
			{
				client.BaseAddress = new Uri(baseUrl);
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			})
			.AddPolicyHandler(GetRetryPolicy())
			.AddPolicyHandler(GetTimeoutPolicy())
			.AddPolicyHandler(GetCircuitBreakerPolicy());
		}

		// resilient client with statistics tracking
		public static void AddResilientHttpClientWithStats<TClient>(
			this IServiceCollection services,
			string baseUrl,
			string apiName)
			where TClient : class
		{
			services.AddHttpClient<TClient>(client =>
			{
				client.BaseAddress = new Uri(baseUrl);
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			})
			// ⭐ ADD STATISTICS HANDLER FIRST (outermost layer)
			.AddHttpMessageHandler(sp =>
			{
				var statsService = sp.GetRequiredService<IStatisticsService>();
				return new StatisticsTrackingHandler(statsService, apiName);
			})
			// Then add resilience policies
			.AddPolicyHandler(GetRetryPolicy())
			.AddPolicyHandler(GetTimeoutPolicy())
			.AddPolicyHandler(GetCircuitBreakerPolicy());
		}

		/// <summary>
		/// Defines a retry policy that retries on transient HTTP errors and 429 responses. 
		/// </summary>		
		private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
			HttpPolicyExtensions
				.HandleTransientHttpError()
				.OrResult(msg => (int)msg.StatusCode == 429)
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

		/// <summary>
		/// Creates and returns an asynchronous timeout policy for HTTP requests.
		/// </summary>
		/// <remarks>The policy enforces a timeout of 10 seconds for each HTTP request. If a request exceeds this
		/// duration,  the operation is canceled, and a timeout exception is thrown.</remarks>
		/// <returns>An <see cref="IAsyncPolicy{HttpResponseMessage}"/> that applies a 10-second timeout to HTTP requests.</returns>
		private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
			Policy.TimeoutAsync<HttpResponseMessage>(10);

		/// <summary>
		/// Creates and returns a circuit breaker policy for handling transient HTTP errors.
		/// </summary>
		/// <remarks>The policy triggers a circuit break after 3 consecutive transient HTTP errors and remains open
		/// for 30 seconds.  While the circuit is open, all requests will fail immediately. After the open period, the circuit
		/// transitions  to a half-open state, allowing a limited number of requests to test if the underlying issue has been
		/// resolved.</remarks>
		/// <returns>An asynchronous circuit breaker policy configured to handle transient HTTP errors.</returns>
		private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
			HttpPolicyExtensions
				.HandleTransientHttpError()
				.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));
	}
}
