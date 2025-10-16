namespace APIAggregator.API.Infrastructure.Http
{
	using Microsoft.Extensions.DependencyInjection;
	using Polly;
	using Polly.Extensions.Http;
	using System.Net.Http.Headers;

	/// <summary>
	/// Provides centralized configuration for resilient <see cref="HttpClient"/> instances.
	/// Registers common policies (retry, timeout, circuit breaker) using Polly and 
	/// allows consistent setup of external API clients through dependency injection.
	/// </summary>
	public static class HttpClientConfiguration
	{
		public static void AddResilientHttpClient<TClient>(this IServiceCollection services, string baseUrl)
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

		private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
			HttpPolicyExtensions
				.HandleTransientHttpError()
				.OrResult(msg => (int)msg.StatusCode == 429)
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

		private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
			Policy.TimeoutAsync<HttpResponseMessage>(10);

		private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
			HttpPolicyExtensions
				.HandleTransientHttpError()
				.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));
	}
}
