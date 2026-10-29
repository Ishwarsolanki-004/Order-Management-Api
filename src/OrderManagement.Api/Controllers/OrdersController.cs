using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.DTOs;
using OrderManagement.Api.Services;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    public OrdersController(IOrderService orders) => _orders = orders;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Place(PlaceOrderRequest request)
        => Ok(await _orders.PlaceOrderAsync(UserId, request));

    [HttpGet("my")]
    public async Task<ActionResult<List<OrderResponse>>> MyOrders()
        => Ok(await _orders.GetMyOrdersAsync(UserId));

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<OrderResponse>>> All()
        => Ok(await _orders.GetAllOrdersAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
        => Ok(await _orders.GetByIdAsync(id, UserId, IsAdmin));

    [HttpPut("{id:int}/status"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, UpdateOrderStatusRequest request)
        => Ok(await _orders.UpdateStatusAsync(id, request.Status));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(int id)
        => Ok(await _orders.CancelAsync(id, UserId, IsAdmin));
}
