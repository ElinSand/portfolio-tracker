using System.Collections.Generic;
using System.Linq;
using PortfolioTracker.API.Models;

namespace PortfolioTracker.API.Services
{
  
    /// Hjälparklass för att räkna ut kostnadsbas (genomsnittligt inköpspris)
    /// utan risk för division med noll.
    public static class CostBasisCalculator
    {
       
        /// Tar en lista av köp‐transaktioner (alla med Type == "Buy") 
        /// och returnerar genomsnittligt inköpspris (decimal).
        /// Om totalQuantity är 0 returneras 0.
        public static decimal CalculateAveragePrice(IEnumerable<Transaction> buyTransactions)
        {
            // Summera totala enheter
            var totalQuantity = buyTransactions.Sum(t => t.Quantity);
            if (totalQuantity <= 0)
            {
                // Inga giltiga köp => inget att dividera med
                return 0m;
            }

            // Summera totala kostnaden (antal × pris per enhet)
            var totalCost = buyTransactions.Sum(t => t.Quantity * t.PriceAtTransaction);

            // Returnera genomsnittligt pris
            return totalCost / totalQuantity;
        }
    }
}

