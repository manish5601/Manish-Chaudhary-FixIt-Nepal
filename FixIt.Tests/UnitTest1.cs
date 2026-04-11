using FixItNepal.Models;

namespace FixIt.Tests
{
    public class ApiResponseTests
    {
        [Fact]
        public void SuccessResponse_ShouldReturnSuccessTrue()
        {
            // Arrange
            var data = "test data";
            var message = "Success message";

            // Act
            var result = ApiResponse<string>.SuccessResponse(data, message);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(message, result.Message);
            Assert.Equal(data, result.Data);
        }

        [Fact]
        public void ErrorResponse_ShouldReturnSuccessFalse()
        {
            // Arrange
            var message = "Error message";

            // Act
            var result = ApiResponse<string>.ErrorResponse(message);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(message, result.Message);
            Assert.Null(result.Data);
        }
    }
}

