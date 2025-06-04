using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioTracker.API.Data;
using PortfolioTracker.API.Models;
using PortfolioTracker.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioTracker.Tests
{
    public class PortfolioServiceTests
    {
        private AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetPortfolioAsync_CorrectlyCalculatesAveragePriceAndValue()
        {
            // Arrange: in-memory DB + ett user + några transaktioner
            var context = CreateInMemoryContext();
            var user = new ApplicationUser
            {
                Id = "user123",
                Balance = 1000m
            };
            context.Users.Add(user);

            // Köp 1 * 100, köp 2 * 200, sälj 1 * 200
            // Totalköp = 100 + 400 = 500 för 3 enheter (före sälj). Snitt = 500/3 = ~166.6667
            // Efter sälj (1 * 200) återstår 2 enheter, men vi räknar snitt på alla köp‐poster (buy‐listan)
            context.Transactions.AddRange(new List<Transaction>
            {
                new Transaction { UserId = "user123", Symbol = "SYM", Type = "Buy",  Quantity = 1m, PriceAtTransaction = 100m },
                new Transaction { UserId = "user123", Symbol = "SYM", Type = "Buy",  Quantity = 2m, PriceAtTransaction = 200m },
                new Transaction { UserId = "user123", Symbol = "SYM", Type = "Sell", Quantity = 1m, PriceAtTransaction = 200m },
            });
            await context.SaveChangesAsync();

            // Mocka BinanceService så att priset för "SYM" alltid är 250
            var mockBinance = new Mock<IBinanceService>();
            mockBinance
                .Setup(x => x.GetCryptoPrice("SYM"))
                .ReturnsAsync(250m);

            var mockLogger = new Mock<ILogger<PortfolioService>>();
            var service = new PortfolioService(context, mockBinance.Object, mockLogger.Object);

            // Act
            var result = await service.GetPortfolioAsync("user123");

            // Assert: holdings
            Assert.Single(result.Holdings);
            var holding = result.Holdings.First();
            // Efter en Sell på 1syms över 3systotal, kvar = 2 enheter
            Assert.Equal(2m, holding.Quantity);

            // AverageBuyPrice = (1*100 + 2*200) / 3 = 500/3
            var expectedAvg = 500m / 3m;
            Assert.Equal(expectedAvg, holding.AverageBuyPrice, precision: 5);

            // CurrentPrice = 250, TotalValue = 2 * 250 = 500
            Assert.Equal(250m, holding.CurrentPrice);
            Assert.Equal(500m, holding.TotalValue);

            // PortfolioValue (eftersom vi separerade bort balans) = 500
            Assert.Equal(500m, result.PortfolioValue);

            // Balance ska fortfarande vara 1000
            Assert.Equal(1000m, result.Balance);

            // Transaktionslista ska innehålla alla tre ursprungliga rader
            Assert.Equal(3, result.Transactions.Count);
        }
    }
}

