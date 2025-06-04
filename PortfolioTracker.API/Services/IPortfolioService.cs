using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioTracker.API.Controllers;
using PortfolioTracker.API.Models;

namespace PortfolioTracker.API.Services
{
    
    /// Definierar alla affärsmetoder som rör portfölj – hämta portfölj, köp, sälj och tillgängliga priser.
    public interface IPortfolioService
    {
        Task<PortfolioDto> GetPortfolioAsync(string userId);
        Task<PortfolioValueDto> GetPortfolioValueAsync(string userId);
        Task<TransactionDto> ExecuteBuyAsync(string userId, TradeModel model);
        Task<TransactionDto> ExecuteSellAsync(string userId, TradeModel model);
        Task<List<AssetPriceDto>> GetAllAssetPricesAsync(string symbol = null);
    }

    public class PortfolioDto
    {
        public decimal Balance { get; set; }
        public decimal PortfolioValue { get; set; }
        public List<HoldingDto> Holdings { get; set; }
        public List<TransactionDto> Transactions { get; set; }
    }

    public class PortfolioValueDto
    {
        public decimal Balance { get; set; }
        public decimal PortfolioValue { get; set; }
        public List<HoldingDto> Holdings { get; set; }
    }

    public class HoldingDto
    {
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal ChangePercent { get; set; }
    }

    public class TransactionDto
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal PriceAtTransaction { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class AssetPriceDto
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
    }
}
