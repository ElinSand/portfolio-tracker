using Microsoft.AspNetCore.Identity;


namespace PortfolioTracker.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public decimal Balance { get; set; }
    }
}
