//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore;
//using PortfolioTracker.API.Models;
//using System.Collections.Generic;

//namespace PortfolioTracker.API.Data
//{
//    public class AppDbContext : IdentityDbContext<ApplicationUser>
//    {
//        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
//        public DbSet<Asset> Assets { get; set; }
//        public DbSet<Transaction> Transactions { get; set; }
//    }
//}



using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.API.Models;
using System.Collections.Generic;

namespace PortfolioTracker.API.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Lägger till decimal-precision för att undvika varningar i loggarna
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.PriceAtTransaction)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Quantity)
                .HasPrecision(18, 8); // Viktigt för kryptotransaktioner!
        }
    }
}
