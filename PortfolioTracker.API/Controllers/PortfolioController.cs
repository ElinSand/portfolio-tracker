//using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.API.Data;
using PortfolioTracker.API.Models;
using PortfolioTracker.API.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;


namespace PortfolioTracker.API.Controllers
{

    [Route("api/portfolio")]
    [ApiController]
    [Authorize] // Kräver inloggning
    public class PortfolioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PortfolioController> _logger;
        private readonly IBinanceService _binanceService;



        public PortfolioController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<PortfolioController> logger, IBinanceService binanceService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _binanceService = binanceService;
        }



        [HttpGet]
        public async Task<IActionResult> GetPortfolio([FromServices] IBinanceService binanceService)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            //Hämtar användarens transaktioner
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();

            //Hämtar aktuell balans
            var balance = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Balance)
                .FirstOrDefaultAsync();

            //Beräknar aktuellt värde på innehav
            var holdings = transactions
                .Where(t => t.Type == "Buy")
                .GroupBy(t => t.Symbol)
                .Select(group => new
                {
                    Symbol = group.Key,
                    Quantity = group.Sum(t => t.Quantity) - transactions
                        .Where(t => t.Type == "Sell" && t.Symbol == group.Key)
                        .Sum(t => t.Quantity)
                })
                .Where(h => h.Quantity > 0)
                .ToList();

            decimal totalPortfolioValue = 0;
            var detailedHoldings = new List<object>();

            foreach (var holding in holdings)
            {
                var price = await binanceService.GetCryptoPrice(holding.Symbol);
                if (price != null)
                {
                    var value = holding.Quantity * price.Value;
                    totalPortfolioValue += value;

                    detailedHoldings.Add(new
                    {
                        holding.Symbol,
                        holding.Quantity,
                        CurrentPrice = price.Value,
                        TotalValue = value
                    });
                }
            }

            return Ok(new
            {
                Balance = balance,
                PortfolioValue = totalPortfolioValue,
                Holdings = detailedHoldings,
                Transactions = transactions.Select(t => new
                {
                    t.Id,
                    t.Symbol,
                    t.Type,
                    t.Quantity,
                    t.PriceAtTransaction,
                    t.Timestamp
                })
            });
        }




        [HttpPost("buy")]
        public async Task<IActionResult> BuyAsset([FromBody] TradeModel model, [FromServices] IBinanceService binanceService)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized access attempt to BuyAsset.");
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return NotFound("User not found.");
            }

            //Hämtar aktuellt pris från Binance API med loggning
            var livePrice = await binanceService.GetCryptoPrice(model.Symbol);
            if (livePrice == null)
            {
                _logger.LogWarning($"Failed to fetch price for {model.Symbol}");
                return BadRequest("Failed to fetch price from Binance.");
            }

            var totalCost = model.Quantity * livePrice.Value;
            if (user.Balance < totalCost)
            {
                _logger.LogWarning($"User {userId} has insufficient balance. Balance: {user.Balance}, Required: {totalCost}");
                return BadRequest("Insufficient balance.");
            }

            //Uppdaterar användarens saldo och loggar köpet
            user.Balance -= totalCost;

            var transaction = new Transaction
            {
                UserId = userId,
                Symbol = model.Symbol,
                Type = "Buy",
                Quantity = model.Quantity,
                PriceAtTransaction = livePrice.Value,
                Timestamp = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} bought {model.Quantity} {model.Symbol} at {livePrice.Value}. New balance: {user.Balance}");

            return Ok(new
            {
                Message = "Purchase successful!",
                Symbol = model.Symbol,
                Quantity = model.Quantity,
                PriceUsed = livePrice.Value,
                TotalCost = totalCost,
                NewBalance = user.Balance
            });
        }



        [HttpPost("sell")]
        public async Task<IActionResult> SellAsset([FromBody] TradeModel model, [FromServices] IBinanceService binanceService)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized access attempt to SellAsset.");
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return NotFound("User not found.");
            }

            //Hämtar aktuellt pris från Binance API och loggar det
            var livePrice = await binanceService.GetCryptoPrice(model.Symbol);
            if (livePrice == null)
            {
                _logger.LogWarning($"Failed to fetch price for {model.Symbol}");
                return BadRequest("Failed to fetch price from Binance.");
            }

            //Räknar hur många enheter användaren äger
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
                return BadRequest("Not enough shares to sell.");
            }

            //Uppdaterar användarens saldo baserat på det aktuella priset
            var totalRevenue = model.Quantity * livePrice.Value;
            user.Balance += totalRevenue;

            //Sparar sälj-transaktionen med det aktuella priset
            var transaction = new Transaction
            {
                UserId = userId,
                Symbol = model.Symbol,
                Type = "Sell",
                Quantity = model.Quantity,
                PriceAtTransaction = livePrice.Value, //Använder aktuellt pris från Binance
                Timestamp = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} sold {model.Quantity} {model.Symbol} at {livePrice.Value}. New balance: {user.Balance}");

            return Ok(new
            {
                Message = "Sale successful!",
                Symbol = model.Symbol,
                Quantity = model.Quantity,
                PriceUsed = livePrice.Value,
                TotalRevenue = totalRevenue,
                NewBalance = user.Balance
            });
        }




        [HttpGet("value")]
        public async Task<IActionResult> GetPortfolioValue([FromServices] IBinanceService binanceService)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            //Hämtar användarens saldo
            var balance = user.Balance;

            //Hämtar användarens innehav (köpta minus sålda)
            var holdings = _context.Transactions
                .Where(t => t.UserId == userId && t.Type == "Buy")
                .GroupBy(t => t.Symbol)
                .Select(group => new
                {
                    Symbol = group.Key,
                    Quantity = group.Sum(t => t.Quantity) - _context.Transactions
                        .Where(t => t.UserId == userId && t.Symbol == group.Key && t.Type == "Sell")
                        .Sum(t => t.Quantity)
                })
                .Where(h => h.Quantity > 0)
                .ToList();

            decimal totalPortfolioValue = balance; // Börjar med användarens saldo
            var detailedHoldings = new List<object>();

            foreach (var holding in holdings)
            {
                var price = await binanceService.GetCryptoPrice(holding.Symbol);
                if (price != null)
                {
                    var value = holding.Quantity * price.Value;
                    totalPortfolioValue += value;

                    detailedHoldings.Add(new
                    {
                        holding.Symbol,
                        holding.Quantity,
                        CurrentPrice = price.Value,
                        TotalValue = value
                    });
                }
            }

            return Ok(new
            {
                PortfolioValue = totalPortfolioValue,
                Balance = balance,
                Holdings = detailedHoldings
            });
        }


        [HttpGet("assets/prices")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPopularAssetsWithPrices()
        {
            var assets = new List<string> { "BTC", "ETH", "BNB", "ADA", "XRP", "SOL", "ADA", "LUNA" }; // Exempel på populära assets
            var assetPrices = new Dictionary<string, decimal>();

            foreach (var asset in assets)
            {
                var price = await _binanceService.GetCryptoPrice(asset);
                if (price.HasValue)
                {
                    assetPrices[asset] = price.Value;
                }
            }

            return Ok(assetPrices);
        }


        //Nytt

        //[HttpGet("assets")]
        //[AllowAnonymous]
        //public async Task<IActionResult> GetAvailableAssetPrices([FromServices] IBinanceService binanceService)
        //{
        //    var assets = await binanceService.GetAllAssetPricesAsync();
        //    return Ok(assets);
        //}
        [HttpGet("assets")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableAssetPrices(
        [FromQuery] string? symbol,
        [FromServices] IBinanceService binanceService)
        {
            var allAssets = await binanceService.GetAllAssetPricesAsync();

            if (!string.IsNullOrEmpty(symbol))
            {
                var filtered = allAssets
                    .Where(a => a.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!filtered.Any())
                    return NotFound($"No asset found with symbol: {symbol}");

                return Ok(filtered);
            }

            return Ok(allAssets);
        }



    }

    // DTO för köp/sälj
    public class TradeModel
    {
        //[Required(ErrorMessage = "Symbol is required")]
        //[RegularExpression("^[A-Z0-9]+$", ErrorMessage = "Symbol must be uppercase letters and numbers only")]
        //public string Symbol { get; set; }

        //[Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
        //public decimal Quantity { get; set; }

        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        //public decimal? Price { get; set; } // Detta kommer senare från API-integration
    }

}
