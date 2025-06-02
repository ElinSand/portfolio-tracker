using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using PortfolioTracker.Frontend.Models.Dto;

namespace PortfolioTracker.Frontend.Pages.Portfolio
{
    public class SellModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public List<Holding> Holdings { get; set; } = new();

        public SellModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient();
            string jwt = HttpContext.Session.GetString("JWToken");

            if (!string.IsNullOrEmpty(jwt))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync("https://localhost:7293/api/portfolio");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PortfolioDto>(json);

                Holdings = data.Holdings;
            }
        }
    }
}

