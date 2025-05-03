//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace PortfolioTracker.Frontend.Pages.Portfolio
//{
//    public class PortfolioModel : PageModel
//    {
//        public void OnGet()
//        {
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using PortfolioTracker.Frontend.Models.Dto;
using Microsoft.AspNetCore.Http;


namespace PortfolioTracker.Frontend.Pages.Portfolio
{

    public class PortfolioModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public decimal Balance { get; set; }
        public decimal PortfolioValue { get; set; }
        public List<Holding> Holdings { get; set; }
        public List<Transaction> Transactions { get; set; }

        public PortfolioModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient();
            string jwt = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(jwt))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            }
            var response = await client.GetAsync("https://localhost:7293/api/portfolio");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PortfolioDto>(json);

                Balance = data.Balance;
                PortfolioValue = data.PortfolioValue;
                Holdings = data.Holdings;
                Transactions = data.Transactions;
            }
            else
            {
                Balance = 0;
                PortfolioValue = 0;
                Holdings = new();
                Transactions = new();
            }
        }
    }
}

