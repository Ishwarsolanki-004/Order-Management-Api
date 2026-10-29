# Order Management API (ASP.NET Core)

E-commerce backend: users register/login (JWT), browse products, place orders with stock checks, and admins manage products and order status.

Stack: C#, ASP.NET Core Web API, Entity Framework Core, SQL Server (LocalDB), JWT, Swagger, xUnit

## Run in Visual Studio
1. Install the .NET 8 SDK and Visual Studio with the "ASP.NET and web development" workload (LocalDB is included with Visual Studio).
2. Open src/OrderManagement.Api/OrderManagement.Api.csproj.
3. In appsettings.json, change Jwt:Key to a long random text (32+ characters).
4. Open Tools > Command Line > Developer PowerShell and run:
   dotnet tool install --global dotnet-ef
   cd src\OrderManagement.Api
   dotnet ef migrations add InitialCreate
5. Press F5. Swagger opens at http://localhost:5090/swagger.
   Default admin: admin@shop.com / Admin@123

## Test flow in Swagger
1. POST /api/auth/login with the admin email and password. Copy the token.
2. Click Authorize (top right), paste the token, click Authorize.
3. GET /api/products to see the sample products.
4. POST /api/orders: {"items":[{"productId":1,"quantity":2}]}
5. GET /api/orders/my, then POST /api/orders/{id}/cancel and check the stock again.

## Tests
dotnet test tests/OrderManagement.Tests
