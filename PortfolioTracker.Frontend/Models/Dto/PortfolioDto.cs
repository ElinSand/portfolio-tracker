using System.Transactions;

namespace PortfolioTracker.Frontend.Models.Dto
{
    public class PortfolioDto
    {
        public decimal Balance { get; set; }
        public decimal PortfolioValue { get; set; }
        public List<Holding> Holdings { get; set; }
        public List<Transaction> Transactions { get; set; }
    }

    public class Holding
    {
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal PriceAtTransaction { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
