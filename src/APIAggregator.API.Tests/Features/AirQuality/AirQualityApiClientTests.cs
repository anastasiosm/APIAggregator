using APIAggregator.API.Features.AirQuality;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace APIAggregator.API.Tests.Features.AirQuality
{
	/*
1. Constructor Tests ✅
•	Valid parameters → successful instantiation
•	Null checks για όλα τα dependencies
•	Empty API key validation
•	Name property verification
2. BuildAirQualityUrl Tests ✅
•	Correct URL formation
•	Proper coordinate formatting
•	API key inclusion
3. MapToAirQualityDto Tests ✅
•	Valid response mapping
•	Null response handling
•	Empty list handling
•	Null list handling
4. GetAirQualityAsync Tests ✅
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
	public class AirQualityApiClientTests
	{
		private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
		private readonly Mock<ILogger<AirQualityApiClient>> _loggerMock;
		private readonly AirQualityApiOptions _options;
		private readonly HttpClient _httpClient;

		public AirQualityApiClientTests()
		{
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
			_loggerMock = new Mock<ILogger<AirQualityApiClient>>();

			_options = new AirQualityApiOptions
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
			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Assert
			Assert.NotNull(client);
			Assert.Equal("AirQuality", client.Name);
		}

		[Fact]
		public void Constructor_NullHttpClient_ThrowsArgumentNullException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentNullException>(() =>
				new AirQualityApiClient(
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
				new AirQualityApiClient(
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
				new AirQualityApiClient(
					_httpClient,
					Options.Create(_options),
					null!));

			Assert.Equal("logger", exception.ParamName);
		}

		[Fact]
		public void Constructor_EmptyApiKey_ThrowsInvalidOperationException()
		{
			// Arrange
			var invalidOptions = new AirQualityApiOptions
			{
				ApiKey = "",
				BaseUrl = "https://api.test.com/"
			};

			// Act & Assert
			var exception = Assert.Throws<InvalidOperationException>(() =>
				new AirQualityApiClient(
					_httpClient,
					Options.Create(invalidOptions),
					_loggerMock.Object));

			Assert.Contains("API key missing", exception.Message);
		}

		#endregion

		#region BuildAirQualityUrl Tests

		[Fact]
		public void BuildAirQualityUrl_ValidCoordinates_ReturnsCorrectUrl()
		{
			// Arrange
			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);
			var lat = 40.7128;
			var lon = -74.0060;

			// Act
			var url = client.BuildAirQualityUrl(lat, lon);

			// Assert
			Assert.Contains("https://api.test.com/data/2.5/air_pollution", url);
			Assert.Contains("lat=40.7128", url);
			Assert.Contains("lon=-74.006", url);
			Assert.Contains("appid=test-api-key", url);
		}

		#endregion

		#region MapToAirQualityDto Tests

		[Fact]
		public void MapToAirQualityDto_ValidResponse_ReturnsDto()
		{
			// Arrange
			var response = new AirQualityApiClient.AirQualityApiResponse
			{
				List = new[]
				{
					new AirQualityApiClient.AirQualityRecord
					{
						Main = new AirQualityApiClient.MainInfo { Aqi = 3 },
						Components = new AirQualityApiClient.ComponentsInfo
						{
							Pm25 = 12.5,
							Pm10 = 20.0
						}
					}
				}
			};

			// Act
			var result = AirQualityApiClient.MapToAirQualityDto(response);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(3, result.AQI);
			Assert.Equal(12.5, result.PM25);
			Assert.Equal(20.0, result.PM10);
		}

		[Fact]
		public void MapToAirQualityDto_NullResponse_ReturnsNull()
		{
			// Act
			var result = AirQualityApiClient.MapToAirQualityDto(null);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void MapToAirQualityDto_EmptyList_ReturnsNull()
		{
			// Arrange
			var response = new AirQualityApiClient.AirQualityApiResponse
			{
				List = Array.Empty<AirQualityApiClient.AirQualityRecord>()
			};

			// Act
			var result = AirQualityApiClient.MapToAirQualityDto(response);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void MapToAirQualityDto_NullList_ReturnsNull()
		{
			// Arrange
			var response = new AirQualityApiClient.AirQualityApiResponse
			{
				List = null!
			};

			// Act
			var result = AirQualityApiClient.MapToAirQualityDto(response);

			// Assert
			Assert.Null(result);
		}

		#endregion

		#region GetAirQualityAsync Tests

		[Fact]
		public async Task GetAirQualityAsync_SuccessfulResponse_ReturnsAirQualityDto()
		{
			// Arrange
			var apiResponse = new
			{
				list = new[]
				{
					new
					{
						main = new { aqi = 2 },
						components = new { pm2_5 = 8.5, pm10 = 15.0 }
					}
				}
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

			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetAirQualityAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(2, result.AQI);
			Assert.Equal(8.5, result.PM25);
			Assert.Equal(15.0, result.PM10);
		}

		[Fact]
		public async Task GetAirQualityAsync_HttpRequestException_ReturnsNull()
		{
			// Arrange
			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new HttpRequestException("API unavailable"));

			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetAirQualityAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.Null(result);
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching air quality")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task GetAirQualityAsync_UnexpectedException_ReturnsNull()
		{
			// Arrange
			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException("Unexpected error"));

			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetAirQualityAsync(40.7128, -74.0060, CancellationToken.None);

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
		public async Task GetAirQualityAsync_EmptyResponse_ReturnsNull()
		{
			// Arrange
			var apiResponse = new
			{
				list = Array.Empty<object>()
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

			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetAirQualityAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.Null(result);
		}

		#endregion

		#region GetDataAsync Tests

		[Fact]
		public async Task GetDataAsync_SuccessfulResponse_ReturnsAirQualityDto()
		{
			// Arrange
			var apiResponse = new
			{
				list = new[]
				{
					new
					{
						main = new { aqi = 4 },
						components = new { pm2_5 = 25.0, pm10 = 40.0 }
					}
				}
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

			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetDataAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.NotNull(result);
			var airQuality = Assert.IsType<AirQualityDto>(result);
			Assert.Equal(4, airQuality.AQI);
			Assert.Equal(25.0, airQuality.PM25);
			Assert.Equal(40.0, airQuality.PM10);
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

			var client = new AirQualityApiClient(
				_httpClient,
				Options.Create(_options),
				_loggerMock.Object);

			// Act
			var result = await client.GetDataAsync(40.7128, -74.0060, CancellationToken.None);

			// Assert
			Assert.NotNull(result);
			var airQuality = Assert.IsType<AirQualityDto>(result);
			Assert.Equal(0, airQuality.AQI);
			Assert.Equal(0, airQuality.PM25);
			Assert.Equal(0, airQuality.PM10);
		}

		#endregion
	}
}