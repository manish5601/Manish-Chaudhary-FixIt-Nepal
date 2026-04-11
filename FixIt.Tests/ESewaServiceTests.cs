using FixItNepal.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace FixIt.Tests
{
    public class ESewaServiceTests
    {
        private Mock<IOptions<ESewaSettings>> _mockOptions;
        private ESewaSettings _settings;

        public ESewaServiceTests()
        {
            _settings = new ESewaSettings
            {
                ProductCode = "EPAYTEST",
                SecretKey = "8g7h3o8ler89aljm"
            };
            _mockOptions = new Mock<IOptions<ESewaSettings>>();
            _mockOptions.Setup(x => x.Value).Returns(_settings);
        }

        [Fact]
        public void GenerateSignature_ShouldReturnCorrectBase64Hash()
        {
            // Arrange
            var service = new ESewaService(_mockOptions.Object);
            decimal totalAmount = 100;
            string transactionUuid = "test-uuid";
            string productCode = "EPAYTEST";

            // Act
            var signature = service.GenerateSignature(totalAmount, transactionUuid, productCode);

            // Assert
            Assert.NotNull(signature);
            // Verify it's a valid Base64 string
            Assert.True(IsBase64String(signature));
        }

        [Fact]
        public async Task VerifyPaymentAsync_WithCompleteStatus_ShouldReturnTrue()
        {
            // Arrange
            var service = new ESewaService(_mockOptions.Object);
            var json = "{\"status\":\"COMPLETE\",\"total_amount\":\"100.0\",\"transaction_uuid\":\"test-uuid\",\"product_code\":\"EPAYTEST\"}";
            var base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await service.VerifyPaymentAsync(base64Data);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task VerifyPaymentAsync_WithFailedStatus_ShouldReturnFalse()
        {
            // Arrange
            var service = new ESewaService(_mockOptions.Object);
            var json = "{\"status\":\"FAILED\",\"total_amount\":\"100.0\",\"transaction_uuid\":\"test-uuid\",\"product_code\":\"EPAYTEST\"}";
            var base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await service.VerifyPaymentAsync(base64Data);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GenerateSignature_WithMismatchedInputs_ShouldProduceDifferentHash()
        {
            // Arrange
            var service = new ESewaService(_mockOptions.Object);
            decimal amount1 = 100;
            decimal amount2 = 100.01m;
            string uuid = "uuid";
            string product = "product";

            // Act
            var sig1 = service.GenerateSignature(amount1, uuid, product);
            var sig2 = service.GenerateSignature(amount2, uuid, product);

            // Assert
            Assert.NotEqual(sig1, sig2);
        }

        [Fact]
        public async Task VerifyPaymentAsync_WithMalformedData_ShouldReturnFalse()
        {
            // Arrange
            var service = new ESewaService(_mockOptions.Object);
            var malformedBase64 = "ThisIsNotBase64!!!";

            // Act
            var result = await service.VerifyPaymentAsync(malformedBase64);

            // Assert
            Assert.False(result);
        }

        private bool IsBase64String(string base64)
        {
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
