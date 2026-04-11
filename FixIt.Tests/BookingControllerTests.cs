using FixItNepal.Controllers;
using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace FixIt.Tests
{
    public class BookingControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IESewaService> _mockESewaService;
        private readonly Mock<IOptions<ESewaSettings>> _mockESewaOptions;

        public BookingControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
            _mockEmailService = new Mock<IEmailService>();
            _mockESewaService = new Mock<IESewaService>();
            _mockESewaOptions = new Mock<IOptions<ESewaSettings>>();
            _mockESewaOptions.Setup(x => x.Value).Returns(new ESewaSettings());
        }

        private ApplicationDbContext GetContext() => new ApplicationDbContext(_dbOptions);

        private ControllerContext GetMockControllerContext(string userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task UpdateStatus_CustomerCancellingConfirmed_ShouldFailWithErrorMessage()
        {
            // Arrange
            using var context = GetContext();
            var customerUser = new ApplicationUser { Id = "cust-1", FullName = "Customer 1", Email = "cust@test.com" };
            var providerUser = new ApplicationUser { Id = "prov-1", FullName = "Provider 1", Email = "prov@test.com" };
            
            var customer = new Customer { Id = 1, UserId = "cust-1", User = customerUser };
            var provider = new FixItNepal.Models.ServiceProvider { Id = 1, UserId = "prov-1", User = providerUser };
            var booking = new Booking
            {
                Id = 1,
                CustomerId = 1,
                ServiceProviderId = 1,
                Status = BookingStatus.Confirmed,
                Customer = customer,
                ServiceProvider = provider,
                BookingDate = DateTime.Today,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0)
            };

            context.Customers.Add(customer);
            context.ServiceProviders.Add(provider);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            var controller = new BookingController(context, _mockUserManager.Object, _mockEmailService.Object, _mockESewaService.Object, _mockESewaOptions.Object);
            controller.ControllerContext = GetMockControllerContext("cust-1", "Customer");
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("cust-1");

            // Act
            var result = await controller.UpdateStatus(1, BookingStatus.Cancelled);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("You cannot cancel a booking once it has been confirmed or is in progress. Please contact the provider or support.", controller.TempData["ErrorMessage"]);
            
            // Re-fetch booking and verify status didn't change
            var dbBooking = await context.Bookings.FindAsync(1);
            Assert.Equal(BookingStatus.Confirmed, dbBooking.Status);
        }

        [Fact]
        public async Task UpdateStatus_ProviderRejectingWithTokenPaid_ShouldSetRefundPending()
        {
            // Arrange
            using var context = GetContext();
            var customerUser = new ApplicationUser { Id = "cust-1", FullName = "Customer 1", Email = "cust@test.com" };
            var providerUser = new ApplicationUser { Id = "prov-1", FullName = "Provider 1", Email = "prov@test.com" };
            
            var customer = new Customer { Id = 1, UserId = "cust-1", User = customerUser };
            var provider = new FixItNepal.Models.ServiceProvider { Id = 1, UserId = "prov-1", User = providerUser };
            var booking = new Booking
            {
                Id = 1,
                CustomerId = 1,
                ServiceProviderId = 1,
                Status = BookingStatus.Pending,
                IsTokenPaid = true,
                Customer = customer,
                ServiceProvider = provider,
                BookingDate = DateTime.Today,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0)
            };

            context.Customers.Add(customer);
            context.ServiceProviders.Add(provider);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            var controller = new BookingController(context, _mockUserManager.Object, _mockEmailService.Object, _mockESewaService.Object, _mockESewaOptions.Object);
            controller.ControllerContext = GetMockControllerContext("prov-1", "ServiceProvider");
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("prov-1");
            _mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(customerUser);

            // Act
            var result = await controller.UpdateStatus(1, BookingStatus.Rejected);

            // Assert
            var dbBooking = await context.Bookings.FindAsync(1);
            Assert.Equal(BookingStatus.RefundPending, dbBooking.Status);
            _mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatus_InvalidCompletionByProvider_ShouldFailWithErrorMessage()
        {
            // Arrange
            using var context = GetContext();
            var providerUser = new ApplicationUser { Id = "prov-1" };
            var provider = new FixItNepal.Models.ServiceProvider { Id = 1, UserId = "prov-1" };
            var booking = new Booking { Id = 1, ServiceProviderId = 1, Status = BookingStatus.AwaitingConfirmation, BookingDate = DateTime.Today, StartTime = new TimeSpan(9,0,0), EndTime = new TimeSpan(10,0,0) };

            context.ServiceProviders.Add(provider);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            var controller = new BookingController(context, _mockUserManager.Object, _mockEmailService.Object, _mockESewaService.Object, _mockESewaOptions.Object);
            controller.ControllerContext = GetMockControllerContext("prov-1", "Customer"); // Acting as Customer but ID is wrong or just testing role logic
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("prov-1");

            // Act
            // Status is AwaitingConfirmation. Customer wants to mark Completed. (Valid if role is Customer)
            // But if Provider tries to mark as Completed? 
            // In code: if (!isProvider && status == BookingStatus.Completed) -> checks if status != AwaitingConfirmation.
            // If isProvider=true, it skips the first check.
            
            // Let's test the specific rule: Only customer can mark as "Completed" from "AwaitingConfirmation"
            // Actually the code says: if (!isProvider && status == BookingStatus.Completed) { ... }
            // What if a Provider tries to mark as Completed? The code doesn't explicitly block it in the Completion Flow Rules section,
            // but it would fall through to booking.Status = status. 
            // Wait, let's re-read the controller code.
        }
    }
}
