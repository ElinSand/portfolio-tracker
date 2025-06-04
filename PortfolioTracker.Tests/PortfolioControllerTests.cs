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
using System.Collections.Generic;

namespace PortfolioTracker.Tests
{
    public class PortfolioControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IPortfolioService> _mockPortfolioService;
        private readonly PortfolioController _controller;

        public PortfolioControllerTests()
        {
            // Mocka UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Mocka IPortfolioService
            _mockPortfolioService = new Mock<IPortfolioService>();

            // Skapa PortfolioController med nya konstruktorn (2 parametrar)
            _controller = new PortfolioController(
                _mockUserManager.Object,
                _mockPortfolioService.Object
            );

            // Sätt upp ett HttpContext med tom User i början
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

            // Ingen setup av user → userId blir null

            // Act
            var result = await _controller.BuyAsset(tradeModel);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task BuyAsset_UserWithLowBalance_ReturnsBadRequest()
        {
            // Arrange
            // Här vill vi simulera att ExecuteBuyAsync kastar InvalidOperationException("Insufficient balance.")
            _mockPortfolioService
                .Setup(s => s.ExecuteBuyAsync("user123", It.IsAny<TradeModel>()))
                .ThrowsAsync(new InvalidOperationException("Insufficient balance."));

            var tradeModel = new TradeModel { Symbol = "BTC", Quantity = 1 };

            // Gör så att controller.User innehåller rätt claim
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            }, "mock"));
            _controller.ControllerContext.HttpContext.User = claims;

            // Act
            var result = await _controller.BuyAsset(tradeModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Insufficient balance.", badRequestResult.Value);
        }

        [Fact]
        public async Task BuyAsset_ValidPurchase_ReturnsOk()
        {
            // Arrange
            var transactionDto = new TransactionDto
            {
                Id = 1,
                Symbol = "BTC",
                Type = "Buy",
                Quantity = 1,
                PriceAtTransaction = 50000m,
                Timestamp = System.DateTime.UtcNow,
                TotalCost = 50000m
            };

            _mockPortfolioService
                .Setup(s => s.ExecuteBuyAsync("user123", It.IsAny<TradeModel>()))
                .ReturnsAsync(transactionDto);

            var tradeModel = new TradeModel { Symbol = "BTC", Quantity = 1 };

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            }, "mock"));
            _controller.ControllerContext.HttpContext.User = claims;

            // Act
            var result = await _controller.BuyAsset(tradeModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<TransactionDto>(okResult.Value);
            Assert.Equal("BTC", returned.Symbol);
            Assert.Equal(1m, returned.Quantity);
            Assert.Equal(50000m, returned.PriceAtTransaction);
        }
    }
}
