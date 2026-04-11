using FixItNepal.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FixIt.Tests
{
    public class EmailServiceTests
    {
        private readonly Mock<IOptions<EmailSettings>> _mockOptions;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailSettings _settings;

        public EmailServiceTests()
        {
            _settings = new EmailSettings
            {
                SmtpServer = "smtp.test.com",
                SmtpPort = 587,
                SmtpUsername = "user",
                SmtpPassword = "password",
                FromEmail = "no-reply@test.com",
                FromName = "FixIt Test"
            };
            _mockOptions = new Mock<IOptions<EmailSettings>>();
            _mockOptions.Setup(x => x.Value).Returns(_settings);
            _mockLogger = new Mock<ILogger<EmailService>>();
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptyToEmail_ShouldLogWarningAndReturn()
        {
            // Arrange
            var service = new EmailService(_mockOptions.Object, _mockLogger.Object);

            // Act
            await service.SendEmailAsync("", "Subject", "Body");

            // Assert
            // Verify LogWarning was called
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Attempted to send email to an empty address")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_WithInvalidSettings_ShouldLogError()
        {
            // Arrange
            // We can't easily test SmtpClient without a real network or a wrapper
            // but we can test that it catches exceptions and logs them.
            var service = new EmailService(_mockOptions.Object, _mockLogger.Object);

            // Act
            await service.SendEmailAsync("test@example.com", "Subject", "Body");

            // Assert
            // This will likely fail with a SocketException in a real environment, 
            // but in a unit test it should be caught by the try-catch block.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }
    }
}
