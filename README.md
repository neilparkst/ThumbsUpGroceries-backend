## ✅ Requirements

- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) — Project targets `net8.0`, tested with SDK version `8.0.204`
- **SQL Server** — Used via `Microsoft.Data.SqlClient` and `Microsoft.EntityFrameworkCore.SqlServer`
- *(Optional)* **Stripe account** — Required if using `Stripe.net` for payment processing
- *(Optional)* **Swagger UI** — Enabled through `Swashbuckle.AspNetCore` for API documentation

---

### 📦 NuGet Package Dependencies

- `BCrypt.Net-Next` — Password hashing
- `Dapper` — Lightweight ORM
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT authentication
- `Microsoft.Data.SqlClient` — SQL Server connectivity
- `Microsoft.EntityFrameworkCore.SqlServer` — EF Core provider for SQL Server
- `Microsoft.EntityFrameworkCore.Tools` — EF Core CLI tooling (not needed at runtime)
- `Stripe.net` — Stripe API integration
- `Swashbuckle.AspNetCore` — Swagger for API documentation

---

## 🛠️ Installation

```bash
# Clone the repository
git clone https://github.com/neilparkst/ThumbsUpGroceries-backend.git

# Navigate to the backend directory
cd ThumbsUpGroceries-backend
```

---

## ▶️ Running the App (Development)

```bash
# Start the development server
dotnet run
```

Or, if you're using Visual Studio, simply click the **Run** button (green triangle) in the toolbar.

---

## ⚙️ Setup

To run the project correctly, you need to configure the following environment variables:

- `ConnectionStrings:DefaultConnection`
- `Frontend`
- `JwtSettings:Audience`
- `JwtSettings:ExpiryMinutes`
- `JwtSettings:Issuer`
- `JwtSettings:SecretKey`
- `Stripe:CheckoutWebhookSecretKey`
- `Stripe:OrdersWebhookSecretKey`
- `Stripe:SecretKey`

These should be defined either in your environment or in your local settings file (`appsettings.Development.json` or user secrets).

---

## 🧱 Tech Stack

- **ASP.NET Core**
- **Dapper**
- **SQL Server**
- **JWT Authentication**
- **Stripe.net**

---

## 📁 Project Structure

```plaintext
/Controllers         # Handles HTTP requests and routes them to services or repositories
/Data/Models         # Defines data structures and entity models
/Data/Repository     # Contains data access logic (queries, commands, etc.)
```
