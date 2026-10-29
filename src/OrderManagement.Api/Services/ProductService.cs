using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.DTOs;
using OrderManagement.Api.Exceptions;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Services;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetAllAsync(string? search, string? category, int page, int pageSize);
    Task<ProductResponse> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(ProductRequest request);
    Task<ProductResponse> UpdateAsync(int id, ProductRequest request);
    Task DeleteAsync(int id);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ProductResponse>> GetAllAsync(string? search, string? category, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var total = await query.CountAsync();
        var products = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProductResponse>
        {
            Items = products.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw AppException.NotFound("Product not found.");
        return ToResponse(p);
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest r)
    {
        var p = new Product
        {
            Name = r.Name.Trim(), Description = r.Description, Category = r.Category.Trim(),
            Price = r.Price, Stock = r.Stock
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return ToResponse(p);
    }

    public async Task<ProductResponse> UpdateAsync(int id, ProductRequest r)
    {
        var p = await _db.Products.FindAsync(id) ?? throw AppException.NotFound("Product not found.");
        p.Name = r.Name.Trim(); p.Description = r.Description; p.Category = r.Category.Trim();
        p.Price = r.Price; p.Stock = r.Stock;
        await _db.SaveChangesAsync();
        return ToResponse(p);
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id) ?? throw AppException.NotFound("Product not found.");
        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id))
            throw AppException.Conflict("Product is part of existing orders and cannot be deleted.");
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
    }

    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Description = p.Description,
        Category = p.Category, Price = p.Price, Stock = p.Stock
    };
}
