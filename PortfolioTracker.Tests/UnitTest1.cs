using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using PortfolioTracker.API.Controllers;
using PortfolioTracker.API.Data;
using PortfolioTracker.API.Models;
using PortfolioTracker.API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace PortfolioTracker.Tests
{
    public class PortfolioControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IBinanceService> _mockBinanceService;
        private readonly Mock<ILogger<PortfolioController>> _mockLogger;
        private readonly AppDbContext _dbContext;
        private readonly PortfolioController _controller;

        public PortfolioControllerTests()
        {
            // Mocka UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Mocka BinanceService
            _mockBinanceService = new Mock<IBinanceService>();
            _mockBinanceService.Setup(b => b.GetCryptoPrice(It.IsAny<string>()))
                               .ReturnsAsync(50000m); // Exempelpris

            // Mocka Logger
            _mockLogger = new Mock<ILogger<PortfolioController>>();

            // Skapa en in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _dbContext = new AppDbContext(options);

            // Skapa PortfolioController
            _controller = new PortfolioController(
                _dbContext,
                _mockUserManager.Object,
                _mockLogger.Object,
                _mockBinanceService.Object
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task BuyAsset_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var tradeModel = new TradeModel { Symbol = "BTC", Quantity = 0.01m };

            // Act
            var result = await _controller.BuyAsset(tradeModel, _mockBinanceService.Object);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task BuyAsset_UserWithLowBalance_ReturnsBadRequest()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user123", Balance = 10 }; // För lite saldo
            _mockUserManager.Setup(u => u.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            var tradeModel = new TradeModel { Symbol = "BTC", Quantity = 1 };

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            }, "mock"));
            _controller.ControllerContext.HttpContext.User = claims;

            // Act
            var result = await _controller.BuyAsset(tradeModel, _mockBinanceService.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Insufficient balance.", badRequestResult.Value);
        }

        [Fact]
        public async Task BuyAsset_ValidPurchase_SavesTransactionAndReturnsOk()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user123", Balance = 100000 }; // Tillräckligt saldo
            _mockUserManager.Setup(u => u.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            var tradeModel = new TradeModel { Symbol = "BTC", Quantity = 1 };

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            }, "mock"));
            _controller.ControllerContext.HttpContext.User = claims;

            // Act
            var result = await _controller.BuyAsset(tradeModel, _mockBinanceService.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var response = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);

            Assert.NotNull(response);
            Assert.Equal("Purchase successful!", response["Message"].ToString());



            var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.UserId == "user123");
            Assert.NotNull(transaction);
            Assert.Equal("BTC", transaction.Symbol);
            Assert.Equal(1, transaction.Quantity);
            Assert.Equal(50000m, transaction.PriceAtTransaction);
        }
    }
}
