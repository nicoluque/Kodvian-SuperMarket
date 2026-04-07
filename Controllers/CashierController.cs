using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/cashier")]
public class CashierController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly Data.ApplicationDbContext _dbContext;

    public CashierController(ICartService cartService, Data.ApplicationDbContext dbContext)
    {
        _cartService = cartService;
        _dbContext = dbContext;
    }

    [HttpGet("inbox")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<List<CartInboxResponse>>> GetInbox()
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var device = await _dbContext.Devices.FindAsync(deviceId);

        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden acceder a la bandeja de cobro" });

        List<Cart> carts;
        carts = await _cartService.GetSentToCashierForCashRegisterAsync(deviceId);

        var response = new List<CartInboxResponse>();
        foreach (var cart in carts)
        {
            var dev = await _dbContext.Devices.FindAsync(cart.DeviceId);
            response.Add(new CartInboxResponse
            {
                Id = cart.Id,
                DeviceId = cart.DeviceId,
                DeviceName = dev?.DeviceName ?? "Unknown",
                CreatedAt = cart.CreatedAt,
                SentToCashierAt = cart.SentToCashierAt,
                Items = cart.Items.Select(i => new CartItemResponse
                {
                    Id = i.Id,
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Discount = i.Discount,
                    Subtotal = i.Subtotal
                }).ToList()
            });
        }

        return Ok(response);
    }
}
