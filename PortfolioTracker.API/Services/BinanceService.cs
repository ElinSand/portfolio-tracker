using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace PortfolioTracker.API.Services
{
   
    public class BinanceService : IBinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BinanceService> _logger;
        private string sort;

        private const string BinancePriceUrl = "https://api.binance.com/api/v3/ticker/price?symbol=";
        private const string BinanceExchangeInfoUrl = "https://api.binance.com/api/v3/exchangeInfo";
        private const string BinanceAllPricesUrl = "https://api.binance.com/api/v3/ticker/price"; //nytt


        public BinanceService(HttpClient httpClient, ILogger<BinanceService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<decimal?> GetCryptoPrice(string symbol)
        {
            try
            {
                var fullSymbol = symbol.ToUpper() + "USDT";
                var url = BinancePriceUrl + fullSymbol;

                _logger.LogInformation($"Fetching price from Binance: {url}"); //Loggar API-anrop

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Binance API call failed: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var priceData = JsonConvert.DeserializeObject<BinancePriceResponse>(json);

                _logger.LogInformation($"Received price for {symbol}: {priceData?.Price}"); //Loggar priset

                return priceData?.Price;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching price for {symbol}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<string>> GetAvailableAssetsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching available assets from Binance.");

                var response = await _httpClient.GetAsync(BinanceExchangeInfoUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to fetch assets from Binance: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<string>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                var symbols = json["symbols"]
                    .Select(s => s["symbol"].ToString())
                    .ToList();

                _logger.LogInformation($"Fetched {symbols.Count} assets from Binance.");
                return symbols;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching available assets: {ex.Message}");
                return new List<string>();
            }
        }

        //Nytt
        public async Task<List<AssetPriceDto>> GetAllAssetPricesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all prices from Binance...");

                var response = await _httpClient.GetAsync(BinanceAllPricesUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var rawPrices = JsonConvert.DeserializeObject<List<BinancePriceResponseRaw>>(json);

                var prices = rawPrices
                    .Where(p => p.Symbol.EndsWith("USDT"))
                    .Select(p => new AssetPriceDto
                    {
                        Symbol = p.Symbol.Replace("USDT", ""),
                        Price = decimal.Parse(p.Price, CultureInfo.InvariantCulture)
                    })
                    .ToList();


                //Testa senare att få sortering att funka.
                //if (sort == "asc")
                //    prices = prices.OrderBy(p => p.Price).ToList();
                //else if (sort == "desc")
                //    prices = prices.OrderByDescending(p => p.Price).ToList();

                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching all prices: {ex.Message}");
                return new List<AssetPriceDto>();
            }
        }


    }

    public class BinancePriceResponse
    {
        [JsonProperty("price")]
        public decimal Price { get; set; }
    }

    //Nytt
    public class BinancePriceResponseRaw
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }
    }
    //Nytt
    public class AssetPriceDto
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
    }


}



//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//namespace PortfolioTracker.API.Services
//{
//    public interface IBinanceService
//    {
//        Task<decimal?> GetCryptoPrice(string symbol);
//        Task<List<string>> GetAvailableAssetsAsync();
//        Task<List<AssetPriceDto>> GetAllAssetPricesAsync();
//    }

//    public class BinanceService : IBinanceService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly ILogger<BinanceService> _logger;

//        private const string BinancePriceUrl = "https://api.binance.com/api/v3/ticker/price?symbol=";
//        private const string BinanceExchangeInfoUrl = "https://api.binance.com/api/v3/exchangeInfo";
//        private const string BinanceAllPricesUrl = "https://api.binance.com/api/v3/ticker/price";

//        public BinanceService(HttpClient httpClient, ILogger<BinanceService> logger)
//        {
//            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<decimal?> GetCryptoPrice(string symbol)
//        {
//            try
//            {
//                var fullSymbol = symbol.ToUpper() + "USDT";
//                var url = BinancePriceUrl + fullSymbol;

//                _logger.LogInformation($"Fetching price from Binance: {url}");

//                var response = await _httpClient.GetAsync(url);
//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning($"Binance API call failed: {response.StatusCode} - {response.ReasonPhrase}");
//                    return null;
//                }

//                var json = await response.Content.ReadAsStringAsync();
//                var priceData = JsonConvert.DeserializeObject<BinancePriceResponse>(json);

//                _logger.LogInformation($"Received price for {symbol}: {priceData?.Price}");

//                return priceData?.Price;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"Error fetching price for {symbol}: {ex.Message}");
//                return null;
//            }
//        }

//        public async Task<List<string>> GetAvailableAssetsAsync()
//        {
//            try
//            {
//                _logger.LogInformation("Fetching available assets from Binance.");

//                var response = await _httpClient.GetAsync(BinanceExchangeInfoUrl);
//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning($"Failed to fetch assets from Binance: {response.StatusCode} - {response.ReasonPhrase}");
//                    return new List<string>();
//                }

//                var content = await response.Content.ReadAsStringAsync();
//                var json = JObject.Parse(content);

//                var symbols = json["symbols"]
//                    .Select(s => s["symbol"].ToString())
//                    .ToList();

//                _logger.LogInformation($"Fetched {symbols.Count} assets from Binance.");
//                return symbols;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"Error fetching available assets: {ex.Message}");
//                return new List<string>();
//            }
//        }

//        public async Task<List<AssetPriceDto>> GetAllAssetPricesAsync()
//        {
//            try
//            {
//                _logger.LogInformation("Fetching all prices from Binance...");

//                var response = await _httpClient.GetAsync(BinanceAllPricesUrl);
//                response.EnsureSuccessStatusCode();

//                var json = await response.Content.ReadAsStringAsync();
//                var rawPrices = JsonConvert.DeserializeObject<List<BinancePriceResponseRaw>>(json);

//                var prices = rawPrices
//                    .Where(p => p.Symbol.EndsWith("USDT"))
//                    .Select(p => new AssetPriceDto
//                    {
//                        Symbol = p.Symbol.Replace("USDT", ""),
//                        Price = decimal.Parse(p.Price)
//                    })
//                    .ToList();

//                return prices;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"Error fetching all prices: {ex.Message}");
//                return new List<AssetPriceDto>();
//            }
//        }
//    }

//    public class BinancePriceResponse
//    {
//        [JsonProperty("price")]
//        public decimal Price { get; set; }
//    }

//    public class BinancePriceResponseRaw
//    {
//        [JsonProperty("symbol")]
//        public string Symbol { get; set; }

//        [JsonProperty("price")]
//        public string Price { get; set; }
//    }

//    public class AssetPriceDto
//    {
//        public string Symbol { get; set; }
//        public decimal Price { get; set; }
//    }
//}