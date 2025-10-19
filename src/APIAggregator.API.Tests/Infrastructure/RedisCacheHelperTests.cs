using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace APIAggregator.API.Tests.Infrastructure
{
	/*
SetAsync
•	✅ Successful caching με default TTL (10 minutes)
•	✅ Custom TTL configuration
•	✅ Exception handling (returns false + logs warning)
•	✅ Verifies JSON serialization
GetAsync
•	✅ Successful retrieval με deserialization
•	✅ Non-existing key (returns default)
•	✅ Empty string (returns default)
•	✅ Invalid JSON (returns default + logs)
•	✅ Exception handling
RemoveAsync
•	✅ Successful removal
•	✅ Exception handling
	 */
	public class RedisCacheHelperTests
	{
		private readonly Mock<IDistributedCache> _cacheMock;
		private readonly Mock<ILogger<RedisCacheHelper>> _loggerMock;
		private readonly RedisCacheHelper _sut; // System Under Test

		public RedisCacheHelperTests()
		{
			_cacheMock = new Mock<IDistributedCache>();
			_loggerMock = new Mock<ILogger<RedisCacheHelper>>();
			_sut = new RedisCacheHelper(_cacheMock.Object, _loggerMock.Object);
		}

		#region SetAsync Tests

		[Fact]
		public async Task SetAsync_ValidData_ReturnsTrue()
		{
			// Arrange
			var key = "test-key";
			var value = new { Name = "Test", Value = 123 };
			DistributedCacheEntryOptions? capturedOptions = null;

			_cacheMock	
				.Setup(x => x.SetAsync(
					key,
					It.IsAny<byte[]>(),
					It.IsAny<DistributedCacheEntryOptions>(),
					It.IsAny<CancellationToken>()))
				.Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
					(_, _, opts, _) => capturedOptions = opts)
				.Returns(Task.CompletedTask);

			// Act
			var result = await _sut.SetAsync(key, value);

			// Assert
			Assert.True(result);
			_cacheMock.Verify(x => x.SetAsync(
				key,
				It.Is<byte[]>(bytes => 
					Encoding.UTF8.GetString(bytes).Contains("Test") && 
					Encoding.UTF8.GetString(bytes).Contains("123")),
				It.IsAny<DistributedCacheEntryOptions>(),
				It.IsAny<CancellationToken>()), Times.Once);

			// Verify default TTL of 10 minutes
			Assert.NotNull(capturedOptions);
			Assert.Equal(TimeSpan.FromMinutes(10), capturedOptions.AbsoluteExpirationRelativeToNow);
		}

		[Fact]
		public async Task SetAsync_WithCustomTtl_UsesProvidedTtl()
		{
			// Arrange
			var key = "test-key";
			var value = "test-value";
			var customTtl = TimeSpan.FromHours(1);
			DistributedCacheEntryOptions? capturedOptions = null;

			_cacheMock
				.Setup(x => x.SetAsync(
					It.IsAny<string>(),
					It.IsAny<byte[]>(),
					It.IsAny<DistributedCacheEntryOptions>(),
					It.IsAny<CancellationToken>()))
				.Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
					(_, _, opts, _) => capturedOptions = opts)
				.Returns(Task.CompletedTask);

			// Act
			var result = await _sut.SetAsync(key, value, customTtl);

			// Assert
			Assert.True(result);
			Assert.NotNull(capturedOptions);
			Assert.Equal(customTtl, capturedOptions.AbsoluteExpirationRelativeToNow);
		}

		[Fact]
		public async Task SetAsync_CacheThrowsException_ReturnsFalse()
		{
			// Arrange
			var key = "test-key";
			var value = "test-value";

			_cacheMock
				.Setup(x => x.SetAsync(
					It.IsAny<string>(),
					It.IsAny<byte[]>(),
					It.IsAny<DistributedCacheEntryOptions>(),
					It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("Redis connection failed"));

			// Act
			var result = await _sut.SetAsync(key, value);

			// Assert
			Assert.False(result);
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(key)),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region GetAsync Tests

		[Fact]
		public async Task GetAsync_ExistingKey_ReturnsDeserializedValue()
		{
			// Arrange
			var key = "test-key";
			var expectedValue = new TestDto { Name = "Test", Value = 42 };
			var json = JsonSerializer.Serialize(expectedValue);
			var bytes = Encoding.UTF8.GetBytes(json);

			_cacheMock
				.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
				.ReturnsAsync(bytes);

			// Act
			var result = await _sut.GetAsync<TestDto>(key);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(expectedValue.Name, result.Name);
			Assert.Equal(expectedValue.Value, result.Value);
		}

		[Fact]
		public async Task GetAsync_NonExistingKey_ReturnsDefault()
		{
			// Arrange
			var key = "non-existing-key";

			_cacheMock
				.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
				.ReturnsAsync((byte[]?)null);

			// Act
			var result = await _sut.GetAsync<TestDto>(key);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public async Task GetAsync_EmptyByteArray_ReturnsDefault()
		{
			// Arrange
			var key = "empty-key";

			_cacheMock
				.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
				.ReturnsAsync(Array.Empty<byte>());

			// Act
			var result = await _sut.GetAsync<TestDto>(key);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public async Task GetAsync_CacheThrowsException_ReturnsDefault()
		{
			// Arrange
			var key = "test-key";

			_cacheMock
				.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("Redis connection timeout"));

			// Act
			var result = await _sut.GetAsync<TestDto>(key);

			// Assert
			Assert.Null(result);
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(key)),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task GetAsync_InvalidJson_ReturnsDefault()
		{
			// Arrange
			var key = "invalid-json-key";
			var invalidJson = "{ invalid json }";
			var bytes = Encoding.UTF8.GetBytes(invalidJson);

			_cacheMock
				.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
				.ReturnsAsync(bytes);

			// Act
			var result = await _sut.GetAsync<TestDto>(key);

			// Assert
			Assert.Null(result);
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.IsAny<It.IsAnyType>(),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region RemoveAsync Tests

		[Fact]
		public async Task RemoveAsync_ValidKey_ReturnsTrue()
		{
			// Arrange
			var key = "test-key";

			_cacheMock
				.Setup(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			// Act
			var result = await _sut.RemoveAsync(key);

			// Assert
			Assert.True(result);
			_cacheMock.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task RemoveAsync_CacheThrowsException_ReturnsFalse()
		{
			// Arrange
			var key = "test-key";

			_cacheMock
				.Setup(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("Redis unavailable"));

			// Act
			var result = await _sut.RemoveAsync(key);

			// Assert
			Assert.False(result);
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(key)),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region Helper Classes

		private class TestDto
		{
			public string Name { get; set; } = string.Empty;
			public int Value { get; set; }
		}

		#endregion
	}
}
