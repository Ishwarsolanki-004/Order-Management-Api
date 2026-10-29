using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.DTOs;
using OrderManagement.Api.Exceptions;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Services;

public interface IOrderService
{
    Task<OrderResponse> PlaceOrderAsync(int userId, PlaceOrderRequest request);
    Task<List<OrderResponse>> GetMyOrdersAsync(int userId);
    Task<List<OrderResponse>> GetAllOrdersAsync();
    Task<OrderResponse> GetByIdAsync(int orderId, int userId, bool isAdmin);
    Task<OrderResponse> UpdateStatusAsync(int orderId, OrderStatus status);
    Task<OrderResponse> CancelAsync(int orderId, int userId, bool isAdmin);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    public async Task<OrderResponse> PlaceOrderAsync(int userId, PlaceOrderRequest request)
    {
        if (request.Items.Count == 0)
            throw new AppException("Order must contain at least one item.");

        // Merge duplicate product lines, e.g. product 1 twice => one line with summed quantity
        var lines = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        var ids = lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

        var order = new Order { UserId = userId };
        foreach (var line in lines)
        {
            var product = products.FirstOrDefault(p => p.Id == line.ProductId)
                          ?? throw AppException.NotFound($"Product {line.ProductId} not found.");
            if (product.Stock < line.Quantity)
                throw new AppException($"Insufficient stock for '{product.Name}'. Available: {product.Stock}.");

            product.Stock -= line.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = product.Price
            });
        }
        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();   // stock update + order insert happen in one transaction
        return await LoadResponseAsync(order.Id);
    }

    public async Task<List<OrderResponse>> GetMyOrdersAsync(int userId)
    {
        var orders = await OrdersQuery().Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();
        return orders.Select(ToResponse).ToList();
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync()
    {
        var orders = await OrdersQuery().OrderByDescending(o => o.CreatedAt).ToListAsync();
        return orders.Select(ToResponse).ToList();
    }

    public async Task<OrderResponse> GetByIdAsync(int orderId, int userId, bool isAdmin)
    {
        var order = await OrdersQuery().FirstOrDefaultAsync(o => o.Id == orderId)
                    ?? throw AppException.NotFound("Order not found.");
        if (!isAdmin && order.UserId != userId)
            throw AppException.Forbidden("You cannot view this order.");
        return ToResponse(order);
    }

    public async Task<OrderResponse> UpdateStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(orderId) ?? throw AppException.NotFound("Order not found.");
        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
            throw new AppException($"A {order.Status} order cannot be changed.");
        if (status == OrderStatus.Cancelled)
            throw new AppException("Use the cancel endpoint to cancel an order.");

        order.Status = status;
        await _db.SaveChangesAsync();
        return await LoadResponseAsync(orderId);
    }

    public async Task<OrderResponse> CancelAsync(int orderId, int userId, bool isAdmin)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId)
                    ?? throw AppException.NotFound("Order not found.");
        if (!isAdmin && order.UserId != userId)
            throw AppException.Forbidden("You cannot cancel this order.");
        if (order.Status != OrderStatus.Pending)
            throw new AppException("Only pending orders can be cancelled.");

        // Return the stock
        var ids = order.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        foreach (var item in order.Items)
            products.First(p => p.Id == item.ProductId).Stock += item.Quantity;

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        return await LoadResponseAsync(orderId);
    }

    private IQueryable<Order> OrdersQuery() =>
        _db.Orders.AsNoTracking().Include(o => o.Items).ThenInclude(i => i.Product);

    private async Task<OrderResponse> LoadResponseAsync(int orderId)
    {
        var order = await OrdersQuery().FirstAsync(o => o.Id == orderId);
        return ToResponse(order);
    }

    private static OrderResponse ToResponse(Order o) => new()
    {
        Id = o.Id,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        CreatedAt = o.CreatedAt,
        Items = o.Items.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
