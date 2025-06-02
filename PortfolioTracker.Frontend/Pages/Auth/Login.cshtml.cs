using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace PortfolioTracker.Frontend.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        [BindProperty]
        public LoginInput Input { get; set; }

        public string ErrorMessage { get; set; }

        public LoginModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _clientFactory.CreateClient();

            var json = JsonConvert.SerializeObject(Input);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://localhost:7293/api/auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Inloggningen misslyckades. Kontrollera e-post och lösenord.";
                return Page();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<LoginResponse>(responseContent);


            // Spara token i Session
            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("UserEmail", Input.Email);


            Response.Cookies.Append("JWToken", result.Token, new CookieOptions
            {
                HttpOnly = false, // Krävs för åtkomst via JavaScript
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return RedirectToPage("/Portfolio/Portfolio");
        }

        public class LoginInput
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public class LoginResponse
        {
            [JsonProperty("token")]
            public string Token { get; set; }
            
        }
    }

}
