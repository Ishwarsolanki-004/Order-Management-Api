using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.DTOs;
using OrderManagement.Api.Exceptions;
using OrderManagement.Api.Models;
using OrderManagement.Api.Services;
using Xunit;

namespace OrderManagement.Tests;

public class OrderServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Products.Add(new Product { Id = 1, Name = "Mouse", Category = "Accessories", Price = 500m, Stock = 10 });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task PlaceOrder_ReducesStock_AndCalculatesTotal()
    {
        using var db = NewDb();
        var service = new OrderService(db);

        var result = await service.PlaceOrderAsync(1, new PlaceOrderRequest
        {
            Items = { new OrderItemRequest { ProductId = 1, Quantity = 3 } }
        });

        Assert.Equal(1500m, result.TotalAmount);
        Assert.Equal(7, db.Products.Find(1)!.Stock);
    }

    [Fact]
    public async Task PlaceOrder_WithInsufficientStock_Throws()
    {
        using var db = NewDb();
        var service = new OrderService(db);

        await Assert.ThrowsAsync<AppException>(() => service.PlaceOrderAsync(1, new PlaceOrderRequest
        {
            Items = { new OrderItemRequest { ProductId = 1, Quantity = 50 } }
        }));
    }

    [Fact]
    public async Task CancelOrder_RestoresStock()
    {
        using var db = NewDb();
        var service = new OrderService(db);
        var order = await service.PlaceOrderAsync(1, new PlaceOrderRequest
        {
            Items = { new OrderItemRequest { ProductId = 1, Quantity = 4 } }
        });

        await service.CancelAsync(order.Id, 1, false);

        Assert.Equal(10, db.Products.Find(1)!.Stock);
    }
}
