using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PortfolioTracker.API.Models;
using PortfolioTracker.API.Services;
using System.Security.Claims;

namespace PortfolioTracker.API.Controllers
{
    [Route("api/portfolio")]
    [ApiController]
    [Authorize]
    public class PortfolioController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(
            UserManager<ApplicationUser> userManager,
            IPortfolioService portfolioService)
        {
            _userManager = userManager;
            _portfolioService = portfolioService;
        }

        // Hjälpmetod för att få ut userId från JWT-claims
        private string GetUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<IActionResult> GetPortfolio()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            try
            {
                var result = await _portfolioService.GetPortfolioAsync(userId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found.");
            }
        }

        [HttpPost("buy")]
        public async Task<IActionResult> BuyAsset([FromBody] TradeModel model)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            try
            {
                var transactionDto = await _portfolioService.ExecuteBuyAsync(userId, model);
                return Ok(transactionDto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("sell")]
        public async Task<IActionResult> SellAsset([FromBody] TradeModel model)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            try
            {
                var transactionDto = await _portfolioService.ExecuteSellAsync(userId, model);
                return Ok(transactionDto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("value")]
        public async Task<IActionResult> GetPortfolioValue()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            try
            {
                var result = await _portfolioService.GetPortfolioValueAsync(userId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found.");
            }
        }

        [HttpGet("assets")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableAssetPrices([FromQuery] string? symbol)
        {
            var prices = await _portfolioService.GetAllAssetPricesAsync(symbol);
            return Ok(prices);
        }
    }

    // DTO för köp/sälj
    public class TradeModel
    {
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
    }
}
