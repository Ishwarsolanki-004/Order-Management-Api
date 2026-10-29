using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.DTOs;
using OrderManagement.Api.Services;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    // GET /api/products?search=mouse&category=Accessories&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
        string? search, string? category, int page = 1, int pageSize = 10)
        => Ok(await _products.GetAllAsync(search, category, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
        => Ok(await _products.GetByIdAsync(id));

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        var created = await _products.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request)
        => Ok(await _products.UpdateAsync(id, request));

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _products.DeleteAsync(id);
        return NoContent();
    }
}
