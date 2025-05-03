using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PortfolioTracker.Frontend.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public RegisterModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public RegisterInput Input { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (Input.Password != Input.ConfirmPassword)
            {
                ErrorMessage = "Lösenorden matchar inte.";
                return Page();
            }

            var client = _clientFactory.CreateClient();

            var json = JsonConvert.SerializeObject(Input);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://localhost:7293/api/auth/register", content);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Registreringen misslyckades. Försök igen.";
                return Page();
            }

            return RedirectToPage("/Auth/Login");
        }

        public class RegisterInput
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Bekräfta lösenord")]
            public string ConfirmPassword { get; set; }
        }
    }
}

