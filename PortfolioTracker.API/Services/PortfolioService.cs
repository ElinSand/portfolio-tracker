using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioTracker.API.Controllers;
using PortfolioTracker.API.Data;
using PortfolioTracker.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioTracker.API.Services
{
   
    /// Implementation av IPortfolioService: innehåller all affärslogik för portföljhantering.
    public class PortfolioService : IPortfolioService
    {
        private readonly AppDbContext _context;
        private readonly IBinanceService _binanceService;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(
            AppDbContext context,
            IBinanceService binanceService,
            ILogger<PortfolioService> logger)
        {
            _context = context;
            _binanceService = binanceService;
            _logger = logger;
        }


        
        /// Hämtar pris via BinanceService. Kastar InvalidOperationException om ogiltigt eller null.
        private async Task<decimal> FetchValidPriceAsync(string symbol)
        {
            var priceResult = await _binanceService.GetCryptoPrice(symbol);
            if (priceResult == null || priceResult.Value <= 0)
            {
                _logger.LogWarning($"Failed to fetch price or price <= 0 for symbol {symbol}.");
                throw new InvalidOperationException("Failed to fetch price from Binance.");
            }
            return priceResult.Value;
        }


        public async Task<PortfolioDto> GetPortfolioAsync(string userId)
        {
            // Hämtar alla transaktioner för användaren
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();

            // Hämtar saldo
            var user = await _context.Users.FindAsync(userId);
            var balance = user?.Balance ?? 0;


            //Beräkna råa holdings med antal och snittpris
            // Gruppindelning per symbol för köp
            var holdingsRaw = transactions
                .Where(t => t.Type == "Buy")
                .GroupBy(t => t.Symbol)
                .Select(group => new
                {
                    Symbol = group.Key,
                    // Räkna ut totalt antal ägda efter att subtrahera sålda
                    Quantity = group.Sum(t => t.Quantity)
                                - transactions
                                    .Where(t => t.Type == "Sell" && t.Symbol == group.Key)
                                    .Sum(t => t.Quantity),
                    // Använd hjälparmetoden, men skicka endast köp-listan
                    AverageBuyPrice = CostBasisCalculator.CalculateAveragePrice(group)
                })
                .Where(h => h.Quantity > 0)
                .ToList();



            decimal holdingsTotalValue = 0m;
            var holdingDtos = new List<HoldingDto>();

            //Hämta varje innehavs nuvarande pris och räknaut värde + förändring
            foreach (var holding in holdingsRaw)
            {
                var price = await _binanceService.GetCryptoPrice(holding.Symbol);
                if (price != null && price.Value > 0)
                {
                    var currentPrice = price.Value;
                    var value = holding.Quantity * currentPrice;
                    var changePercent = ((currentPrice - holding.AverageBuyPrice) / holding.AverageBuyPrice) * 100;
                    holdingsTotalValue += value;

                    holdingDtos.Add(new HoldingDto
                    {
                        Symbol = holding.Symbol,
                        Quantity = holding.Quantity,
                        CurrentPrice = currentPrice,
                        TotalValue = value,
                        AverageBuyPrice = holding.AverageBuyPrice,
                        ChangePercent = changePercent
                    });
                }
            }

            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Symbol = t.Symbol,
                Type = t.Type,
                Quantity = t.Quantity,
                PriceAtTransaction = t.PriceAtTransaction,
                Timestamp = t.Timestamp,
                TotalCost = t.Quantity * t.PriceAtTransaction
            }).ToList();

            return new PortfolioDto
            {
                Balance = balance,
                PortfolioValue = holdingsTotalValue,
                Holdings = holdingDtos,
                Transactions = transactionDtos
            };
        }

        public async Task<PortfolioValueDto> GetPortfolioValueAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            var balance = user?.Balance ?? 0;

            var holdingsRaw = await _context.Transactions
                .Where(t => t.UserId == userId && t.Type == "Buy")
                .GroupBy(t => t.Symbol)
                .Select(group => new
                {
                    Symbol = group.Key,
                    Quantity = group.Sum(t => t.Quantity)
                                 - _context.Transactions
                                     .Where(t => t.UserId == userId && t.Symbol == group.Key && t.Type == "Sell")
                                     .Sum(t => t.Quantity)
                })
                .Where(h => h.Quantity > 0)
                .ToListAsync();

            decimal totalPortfolioValue = balance;
            var holdingDtos = new List<HoldingDto>();

            foreach (var holding in holdingsRaw)
            {
                var price = await _binanceService.GetCryptoPrice(holding.Symbol);
                if (price != null && price.Value > 0)
                {
                    var currentPrice = price.Value;
                    var value = holding.Quantity * currentPrice;
                    totalPortfolioValue += value;

                    holdingDtos.Add(new HoldingDto
                    {
                        Symbol = holding.Symbol,
                        Quantity = holding.Quantity,
                        CurrentPrice = currentPrice,
                        TotalValue = value,
                        AverageBuyPrice = 0,
                        ChangePercent = 0
                    });
                }
            }

            return new PortfolioValueDto
            {
                Balance = balance,
                PortfolioValue = totalPortfolioValue,
                Holdings = holdingDtos
            };
        }

        public async Task<TransactionDto> ExecuteBuyAsync(string userId, TradeModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }


            // Anropar hjälpmetoden för att hämta pris
            var unitPrice = await FetchValidPriceAsync(model.Symbol);

            var totalCost = model.Quantity * unitPrice;



            if (user.Balance < totalCost)
            {
                _logger.LogWarning($"User {userId} has insufficient balance. Balance: {user.Balance}, Required: {totalCost}");
                throw new InvalidOperationException("Insufficient balance.");
            }

         

            user.Balance -= totalCost;
            _context.Users.Update(user);

            var transaction = new Transaction
            {
                UserId = userId,
                Symbol = model.Symbol,
                Type = "Buy",
                Quantity = model.Quantity,
                PriceAtTransaction = unitPrice,
                Timestamp = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return new TransactionDto
            {
                Id = transaction.Id,
                Symbol = transaction.Symbol,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                PriceAtTransaction = transaction.PriceAtTransaction,
                Timestamp = transaction.Timestamp,
                TotalCost = totalCost
            };
        }

        public async Task<TransactionDto> ExecuteSellAsync(string userId, TradeModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }



            var unitPrice = await FetchValidPriceAsync(model.Symbol);

            var ownedShares = await _context.Transactions
                .Where(t => t.UserId == userId && t.Symbol == model.Symbol && t.Type == "Buy")
                .SumAsync(t => t.Quantity);

            var soldShares = await _context.Transactions
                .Where(t => t.UserId == userId && t.Symbol == model.Symbol && t.Type == "Sell")
                .SumAsync(t => t.Quantity);

            var availableShares = ownedShares - soldShares;
            if (availableShares < model.Quantity)
            {
                _logger.LogWarning($"User {userId} tried to sell {model.Quantity} {model.Symbol} but only owns {availableShares}.");
                throw new InvalidOperationException("Not enough shares to sell.");
            }

            var totalRevenue = model.Quantity * unitPrice; 
            user.Balance += totalRevenue;
            _context.Users.Update(user);

            var transaction = new Transaction
            {
                UserId = userId,
                Symbol = model.Symbol,
                Type = "Sell",
                Quantity = model.Quantity,
                PriceAtTransaction = unitPrice, 
                Timestamp = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return new TransactionDto
            {
                Id = transaction.Id,
                Symbol = transaction.Symbol,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                PriceAtTransaction = transaction.PriceAtTransaction,
                Timestamp = transaction.Timestamp,
                TotalCost = totalRevenue
            };
        }

        public async Task<List<AssetPriceDto>> GetAllAssetPricesAsync(string symbol = null)
        {
            var allAssets = await _binanceService.GetAllAssetPricesAsync();

            if (!string.IsNullOrEmpty(symbol))
            {
                return allAssets
                    .Where(a => a.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return allAssets;
        }
    }
}

