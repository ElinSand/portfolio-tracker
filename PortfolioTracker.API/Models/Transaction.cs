namespace PortfolioTracker.API.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Symbol { get; set; } // Direkt lagrar symbolen istället för AssetId

        public string Type { get; set; } // Buy or Sell
        public decimal Quantity { get; set; }
        public decimal PriceAtTransaction { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
