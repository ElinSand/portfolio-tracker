using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortfolioTracker.API.Services
{
    public interface IBinanceService
    {
        Task<decimal?> GetCryptoPrice(string symbol);
        Task<List<string>> GetAvailableAssetsAsync();
        Task<List<AssetPriceDto>> GetAllAssetPricesAsync(); 

    }
}
