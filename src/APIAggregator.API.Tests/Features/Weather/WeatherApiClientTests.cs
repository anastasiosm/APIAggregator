using APIAggregator.API.Features.Weather;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace APIAggregator.API.Tests.Features.Weather
{
	/*
	1. Constructor Tests ✅
	•	Valid parameters → successful instantiation
	•	Null checks για όλα τα dependencies
	•	Empty API key validation
	•	Name property verification
	2. BuildWeatherUrl Tests ✅
	•	Correct URL formation
	•	Proper coordinate formatting
	•	API key inclusion
	•	Units parameter (metric)
	3. MapToWeatherDto Tests ✅
	•	Valid response mapping
	•	Null response handling
	•	Empty weather array handling
	•	Null weather array handling
	4. GetWeatherAsync Tests ✅
	•	Successful API call
	•	HttpRequestException handling με logging
	•	Generic exception handling με logging
	•	Empty response handling
	5. GetDataAsync Tests ✅
	•	Successful response returns DTO
	•	API failure returns default DTO (fallback)
	🔑 Key Testing Patterns:
	1.	HttpMessageHandler Mocking: Για να mock-άρουμε HTTP calls
	2.	Protected() Setup: Για το SendAsync που είναι protected method
	3.	Logger Verification: Structured logging validation
	4.	AAA Pattern: Arrange → Act → Assert σε όλα τα tests
	*/
	public class WeatherApiClientTests
	{
		private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
		private readonly Mock<ILogger<WeatherApiClient>> _loggerMock;
		private readonly WeatherApiOptions _options;
		private readonly HttpClient _httpClient;

		public WeatherApiClientTests()
		{
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
			_loggerMock = new Mock<ILogger<WeatherApiClient>>();

			_options = new WeatherApiOptions
			{
				ApiKey = "test-api-key",
				BaseUrl = "https://api.test.com/data/2.5/"
			};

			_httpClient = new HttpClient(_httpMessageHandlerMock.Object)
			{
				BaseAddress = new Uri("https://api.test.com/")
			};
		}

		#region Constructor Tests

		[Fact]
		public void Constructor_ValidParameters_CreatesInstance()
		{
			// Arrange & Act
			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Assert
			Assert.NotNull(client);
			Assert.Equal("Weather", client.Name);
		}

		[Fact]
		public void Constructor_NullHttpClient_ThrowsArgumentNullException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentNullException>(() =>
				new WeatherApiClient(
					null!,
					Options.Create(_options),
					_loggerMock.Object));

			Assert.Equal("client", exception.ParamName);
		}

		[Fact]
		public void Constructor_NullOptions_ThrowsArgumentNullException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentNullException>(() =>
				new WeatherApiClient(
					_httpClient,
					null!,
					_loggerMock.Object));

			Assert.Equal("options", exception.ParamName);
		}

		[Fact]
		public void Constructor_NullLogger_ThrowsArgumentNullException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentNullException>(() =>
				new WeatherApiClient(
					_httpClient,
					Options.Create(_options),
					null!));

			Assert.Equal("logger", exception.ParamName);
		}

		[Fact]
		public void Constructor_EmptyApiKey_ThrowsInvalidOperationException()
		{
			// Arrange
			var invalidOptions = new WeatherApiOptions
			{
				ApiKey = "",
				BaseUrl = "https://api.test.com/"
			};

			// Act & Assert
			var exception = Assert.Throws<InvalidOperationException>(() =>
				new WeatherApiClient(
					_httpClient,
					Options.Create(invalidOptions),
					_loggerMock.Object));

			Assert.Contains("API key missing", exception.Message);
		}

		#endregion

		#region BuildWeatherUrl Tests

		[Fact]
		public void BuildWeatherUrl_ValidCoordinates_ReturnsCorrectUrl()
		{
			// Arrange
			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);
			var lat = 40.7128;
			var lon = -74.0060;

			// Act
			var url = client.BuildWeatherUrl(lat, lon);

			// Assert
			Assert.Contains("https://api.test.com/data/2.5/weather", url);
			Assert.Contains("lat=40.7128", url);
			Assert.Contains("lon=-74.006", url);
			Assert.Contains("appid=test-api-key", url);
			Assert.Contains("units=metric", url);
		}

		[Fact]
		public void BuildWeatherUrl_IncludesMetricUnits()
		{
			// Arrange
			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var url = client.BuildWeatherUrl(0, 0);

			// Assert
			Assert.Contains("units=metric", url);
		}

		#endregion

		#region MapToWeatherDto Tests

		[Fact]
		public void MapToWeatherDto_ValidResponse_ReturnsDto()
		{
			// Arrange
			var response = new WeatherApiClient.WeatherApiResponse
			{
				Weather = new[]
				{
					new WeatherApiClient.WeatherInfo
					{
						Main = "Clear",
						Description = "clear sky"
					}
				},
				Main = new WeatherApiClient.MainInfo
				{
					Temp = 22.5
				}
			};

			// Act
			var result = WeatherApiClient.MapToWeatherDto(response);

			// Assert
			Assert.NotNull(result);
			Assert.Equal("Clear", result.Summary);
			Assert.Equal("clear sky", result.Description);
			Assert.Equal(22.5, result.TemperatureC);
		}

		[Fact]
		public void MapToWeatherDto_NullResponse_ReturnsNull()
		{
			// Act
			var result = WeatherApiClient.MapToWeatherDto(null);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void MapToWeatherDto_EmptyWeatherArray_ReturnsNull()
		{
			// Arrange
			var response = new WeatherApiClient.WeatherApiResponse
			{
				Weather = Array.Empty<WeatherApiClient.WeatherInfo>(),
				Main = new WeatherApiClient.MainInfo { Temp = 20.0 }
			};

			// Act
			var result = WeatherApiClient.MapToWeatherDto(response);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void MapToWeatherDto_NullWeatherArray_ReturnsNull()
		{
			// Arrange
			var response = new WeatherApiClient.WeatherApiResponse
			{
				Weather = null!,
				Main = new WeatherApiClient.MainInfo { Temp = 20.0 }
			};

			// Act
			var result = WeatherApiClient.MapToWeatherDto(response);

			// Assert
			Assert.Null(result);
		}

		#endregion

		#region GetWeatherAsync Tests

		[Fact]
		public async Task GetWeatherAsync_SuccessfulResponse_ReturnsWeatherDto()
		{
			// Arrange
			var apiResponse = new
			{
				weather = new[]
				{
					new
					{
						main = "Clouds",
						description = "scattered clouds"
					}
				},
				main = new { temp = 18.3 }
			};

			var json = JsonSerializer.Serialize(apiResponse);
			var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
			};

			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(httpResponse);

			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetWeatherAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.NotNull(result);
			Assert.Equal("Clouds", result.Summary);
			Assert.Equal("scattered clouds", result.Description);
			Assert.Equal(18.3, result.TemperatureC);
		}

		[Fact]
		public async Task GetWeatherAsync_HttpRequestException_ReturnsNull()
		{
			// Arrange
			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new HttpRequestException("API unavailable"));

			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetWeatherAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.Null(result);
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching weather")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task GetWeatherAsync_UnexpectedException_ReturnsNull()
		{
			// Arrange
			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException("Unexpected error"));

			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetWeatherAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.Null(result);
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task GetWeatherAsync_EmptyWeatherArray_ReturnsNull()
		{
			// Arrange
			var apiResponse = new
			{
				weather = Array.Empty<object>(),
				main = new { temp = 20.0 }
			};

			var json = JsonSerializer.Serialize(apiResponse);
			var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
			};

			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(httpResponse);

			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetWeatherAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.Null(result);
		}

		#endregion

		#region GetDataAsync Tests

		[Fact]
		public async Task GetDataAsync_SuccessfulResponse_ReturnsWeatherDto()
		{
			// Arrange
			var apiResponse = new
			{
				weather = new[]
				{
					new
					{
						main = "Rain",
						description = "light rain"
					}
				},
				main = new { temp = 15.7 }
			};

			var json = JsonSerializer.Serialize(apiResponse);
			var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
			};

			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(httpResponse);

			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetDataAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.NotNull(result);
			var weather = Assert.IsType<WeatherDto>(result);
			Assert.Equal("Rain", weather.Summary);
			Assert.Equal("light rain", weather.Description);
			Assert.Equal(15.7, weather.TemperatureC);
		}

		[Fact]
		public async Task GetDataAsync_ApiFailure_ReturnsDefaultDto()
		{
			// Arrange
			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new HttpRequestException("API error"));

			var client = new WeatherApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetDataAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.NotNull(result);
			var weather = Assert.IsType<WeatherDto>(result);
			Assert.Equal("No data", weather.Summary);
			Assert.Equal("No data available", weather.Description);
			Assert.Equal(0, weather.TemperatureC);
		}

		#endregion
	}
}