using FixItNepal.Controllers;
using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace FixIt.Tests
{
    public class ServiceRequestControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IWebHostEnvironment> _mockEnv;

        public ServiceRequestControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(x => x.WebRootPath).Returns("wwwroot");
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
        public async Task SubmitBid_DuplicateBidFromSameProvider_ShouldReject()
        {
            // Arrange
            using var context = GetContext();
            var providerUser = new ApplicationUser { Id = "prov-1", FullName = "Provider 1" };
            var provider = new FixItNepal.Models.ServiceProvider { Id = 1, UserId = "prov-1", User = providerUser };
            var customer = new Customer { Id = 1, UserId = "cust-1" };
            var category = new ServiceCategory { Id = 1, Name = "Plumbing" };
            var request = new ServiceRequest { Id = 1, CustomerId = 1, ServiceCategoryId = 1, ServiceCategory = category, Title = "Leaking Tap", Description = "Fix it please", Status = ServiceRequestStatus.Open };
            var existingBid = new ServiceBid { Id = 1, ServiceRequestId = 1, ServiceProviderId = 1, Status = BidStatus.Pending, ProposedPrice = 500, Notes = "Bid 1" };

            context.Customers.Add(customer);
            context.ServiceCategories.Add(category);
            context.ServiceProviders.Add(provider);
            context.ServiceRequests.Add(request);
            context.ServiceBids.Add(existingBid);
            await context.SaveChangesAsync();

            var controller = new ServiceRequestController(_mockUserManager.Object, context, _mockEnv.Object);
            controller.ControllerContext = GetMockControllerContext("prov-1", "ServiceProvider");
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(providerUser);

            var bidModel = new ServiceBidViewModel
            {
                ServiceRequestId = 1,
                ProposedPrice = 500,
                EstimatedTime = "1 hour",
                Notes = "I can fix it"
            };

            // Act
            var result = await controller.SubmitBid(bidModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("You have already submitted a bid for this request.", controller.TempData["ErrorMessage"]);
            
            // Verify count of bids for this request is still 1
            var bidsCount = await context.ServiceBids.CountAsync(b => b.ServiceRequestId == 1);
            Assert.Equal(1, bidsCount);
        }

        [Fact]
        public async Task SubmitBid_ValidBid_ShouldUpdateStatusToBidding()
        {
            // Arrange
            using var context = GetContext();
            var providerUser = new ApplicationUser { Id = "prov-1", FullName = "Provider 1" };
            var provider = new FixItNepal.Models.ServiceProvider { Id = 1, UserId = "prov-1", User = providerUser };
            var customer = new Customer { Id = 1, UserId = "cust-1" };
            var category = new ServiceCategory { Id = 1, Name = "Plumbing" };
            var request = new ServiceRequest { Id = 1, CustomerId = 1, ServiceCategoryId = 1, ServiceCategory = category, Title = "Leaking Tap", Description = "Fix it please", Status = ServiceRequestStatus.Open };

            context.Customers.Add(customer);
            context.ServiceCategories.Add(category);
            context.ServiceProviders.Add(provider);
            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();

            var controller = new ServiceRequestController(_mockUserManager.Object, context, _mockEnv.Object);
            controller.ControllerContext = GetMockControllerContext("prov-1", "ServiceProvider");
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(providerUser);

            var bidModel = new ServiceBidViewModel
            {
                ServiceRequestId = 1,
                ProposedPrice = 500,
                EstimatedTime = "1 hour",
                Notes = "I can fix it"
            };

            // Act
            var result = await controller.SubmitBid(bidModel);

            // Assert
            var dbRequest = await context.ServiceRequests.FindAsync(1);
            Assert.Equal(ServiceRequestStatus.Bidding, dbRequest.Status);
            
            var dbBid = await context.ServiceBids.FirstOrDefaultAsync(b => b.ServiceRequestId == 1);
            Assert.NotNull(dbBid);
            Assert.Equal(500, dbBid.ProposedPrice);
        }

        [Fact]
        public async Task Edit_RequestWithBids_ShouldRedirectWithErrorMessage()
        {
            // Arrange
            using var context = GetContext();
            var customerUser = new ApplicationUser { Id = "cust-1" };
            var customer = new Customer { Id = 1, UserId = "cust-1" };
            var category = new ServiceCategory { Id = 1, Name = "Plumbing" };
            var request = new ServiceRequest { Id = 1, CustomerId = 1, ServiceCategoryId = 1, ServiceCategory = category, Title = "Title", Description = "Desc" };
            var bid = new ServiceBid { Id = 1, ServiceRequestId = 1, ServiceProviderId = 1, ProposedPrice = 500, Notes = "Notes" };

            context.ServiceCategories.Add(category);
            context.Customers.Add(customer);
            context.ServiceRequests.Add(request);
            context.ServiceBids.Add(bid);
            await context.SaveChangesAsync();

            var controller = new ServiceRequestController(_mockUserManager.Object, context, _mockEnv.Object);
            controller.ControllerContext = GetMockControllerContext("cust-1", "Customer");
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(customerUser);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("You cannot edit a request that already has bids.", controller.TempData["ErrorMessage"]);
        }
    }
}
