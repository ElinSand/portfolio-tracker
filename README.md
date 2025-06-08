# PortfolioTracker
**PortfolioTracker** is a web application that simulates cryptocurrency trading. 
Users can register, log in, and—using a fictional starting balance of $10,000—buy and sell various 
cryptocurrencies at real-time prices fetched from the Binance API. The app displays current balance, 
portfolio value, percentage change per holding, and a complete transaction history.

### Features
1.	**User Management & Authentication**  
-	**Registration** (email + password) using ASP.NET Identity.  
-	**Login** issues a JWT stored in an HttpOnly cookie.
-	**Role-based authorization** ensures all portfolio endpoints require a valid token ([Authorize]).

2.	**Portfolio & Transactions**
-	Starting balance: $10,000.
-	Buy/Sell functionality for USDT trading pairs.
-	Automatic computation of holdings (net quantity), average buy price, and live price.
-	Real-time prices fetched from Binance (single‐symbol lookups or all‐prices endpoint).
-	**Display:**
+	**Balance**
  +	**Portfolio Value** (sum of all holding values)
  +	**Holdings Table:** symbol | quantity | current price | % change | total value | total cost
  +	Transaction History: each buy/sell with timestamp, quantity, price, and total amount.

3.	**Backend Architecture**
-	**Domain:** Core business logic and models (ApplicationUser, Transaction, CostBasisCalculator).
-	**Application:** IPortfolioService / PortfolioService encapsulate all business rules—validations (sufficient balance, owned quantity), computations, and DTO mapping.
-	**Infrastructure:**
  +	**AppDbContext** (Entity Framework Core) with DbSet<Transaction> and proper decimal precision (18,8 for prices/quantities, 18,2 for balances).
  +	**IBinanceService / BinanceService** to fetch individual prices (GetCryptoPrice) or all prices (GetAllAssetPricesAsync).
  +	(Future) Optional IPriceCacheService to cache prices in memory and reduce Binance calls.

4.	**API Endpoints** (PortfolioController)
-	GET /api/portfolio => returns full portfolio (PortfolioDto) with balance, holdings, and transactions.
-	POST /api/portfolio/buy => buys a specified quantity, verifies balance, creates a Transaction.
-	POST /api/portfolio/sell => sells a specified quantity, verifies owned amount, updates balance.
-	GET /api/portfolio/value => returns portfolio value and holdings (no transaction list).
-	GET /api/portfolio/assets?symbol={SYMBOL} => returns all available crypto prices or filters by symbol.

5.	**Frontend** (Razor Pages)
-	**Index:** Displays all available coins and “Buy” buttons.
-	**Auth/Login & Auth/Register:** Razor forms with client-side validation and server-side error handling.
-	**Portfolio:** Shows user’s balance, holdings, portfolio value, and transaction history in a Bootstrap table.
-	**Sell:** Lists holdings in a table; each row has a “Sell” button that opens a small popup form for quantity.

6.	**Security**
-	**HTTPS** everywhere—no unencrypted traffic.
-	**JWT authentication** with HttpOnly cookies to prevent XSS token theft.
-	**Backend validations** in PortfolioService to prevent logic manipulation (e.g., negative balances or selling more than owned).
-	**CORS policy** restricted to the front-end origin.
-	**Input validation** on both client and server (CSRF protection can be added later).
-	Performed a quick **BurpSuite scan** to identify missing input validation and verify JWT protection on endpoints.

7.	**Testing & CI**
-	**Unit Tests** (xUnit + Moq):
  +	PortfolioControllerTests: Verifies Unauthorized, BadRequest, and Ok responses for buy/sell endpoints, and checks that transactions are saved correctly in an in-memory database.
  +	PortfolioServiceTests: Tests business logic in isolation (balance checks, average price calculation, owned quantity).
-	**GitHub Actions** CI Pipeline:
  +	Runs on every push or pull request to main.
  +	Steps:
1.	Check out code (actions/checkout@v4)
2.	Set up .NET (actions/setup-dotnet@v4, .NET 8.x)
3.	dotnet restore
4.	dotnet build
5.	dotnet test
-	Ensures the solution builds and all tests pass before merging.

# Installation & Local Setup
1.	Clone the repo
2.	Configure the database
-	Open PortfolioTracker.API/appsettings.json and ensure DefaultConnection points to your SQL Server instance (e.g., (localdb)\MSSQLLocalDB).
3.	Run the Project
-	To run both the backend and frontend components locally, do the following:
1. Right-click on the solution and select Configure Startup Projects.
2. Choose Multiple startup projects.
3. Set PortfolioTracker.API and PortfolioTracker.Frontend as the startup projects.
4. Save the configuration and run the application.

## Future Improvements
- **Price Caching Service:** Implement an IPriceCacheService to fetch and cache all relevant USDT pairs in the background, reducing per-request API calls to Binance and improving performance.
- **Repository Pattern:** Introduce repository interfaces (e.g., ITransactionRepository) so services do not depend directly on DbContext. This would allow swapping out EF Core for another ORM, adding caching layers, or using a different data store without modifying business logic.
- **Anti-CSRF Protection:** Add antiforgery tokens to all Razor Page forms and validate them server-side.
- **Content Security Policy (CSP):** Enforce a strict CSP header to mitigate XSS risks.
-	**SPA Frontend:** Create a more interactive single-page application (React or Blazor) for a smoother user experience.
-	**Token Refresh:** Implement refresh tokens with shorter‐lived access tokens for improved security.

________________________________________
Contact  

Name: Elin Sand  
GitHub: GitHub  

Feel free to reach out if you have any questions or need further support!
   
Thank you for checking out PortfolioTracker!


