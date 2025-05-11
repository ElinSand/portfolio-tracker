using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using PortfolioTracker.Frontend.Models.Dto;

namespace PortfolioTracker.Frontend.Pages
{
    public class IndexModel : PageModel
    {


        private readonly IHttpClientFactory _clientFactory;

        public List<AssetPriceDto> AvailableAssets { get; set; } = new();
        public bool IsLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString("JWToken"));

        public IndexModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient();
            string jwt = HttpContext.Session.GetString("JWToken"); // om jag använder auth
            if (!string.IsNullOrEmpty(jwt))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync("https://localhost:7293/api/portfolio/assets");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                AvailableAssets = JsonConvert.DeserializeObject<List<AssetPriceDto>>(json);
            }
        }


    }
}
