using OrderManagement.Api.Models;

namespace OrderManagement.Api.Data;

public static class DbSeeder
{
    // Development only: creates a default admin and a few products.
    public static void Seed(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Name = "Admin",
                Email = "admin@shop.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = UserRole.Admin
            });
        }
        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product { Name = "Wireless Mouse", Category = "Accessories", Price = 499m, Stock = 50 },
                new Product { Name = "Mechanical Keyboard", Category = "Accessories", Price = 2499m, Stock = 20 },
                new Product { Name = "USB-C Cable", Category = "Accessories", Price = 199m, Stock = 100 });
        }
        db.SaveChanges();
    }
}
