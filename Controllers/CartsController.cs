using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/carts")]
[DeviceAuth]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost]
    public async Task<ActionResult<CartResponse>> Create()
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        
        var cart = await _cartService.CreateAsync(deviceId);
        
        return Ok(ToCartResponse(cart));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CartResponse>> Get(int id)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var cart = await _cartService.GetByIdAsync(id);

        if (cart == null)
            return NotFound();

        var isOwnerDevice = cart.DeviceId == deviceId;
        var isTargetCashRegister = cart.Status == CartStatus.SentToCashier.ToString()
            && cart.TargetCashRegisterDeviceId == deviceId;

        if (!isOwnerDevice && !isTargetCashRegister)
            return NotFound();

        return Ok(ToCartResponse(cart));
    }

    [HttpGet("{id}/container-check")]
    public async Task<ActionResult<CartContainerCheckResponse>> GetContainerCheck(int id)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var cart = await _cartService.GetByIdAsync(id);

        if (cart == null)
            return NotFound();

        var isOwnerDevice = cart.DeviceId == deviceId;
        var isTargetCashRegister = cart.Status == CartStatus.SentToCashier.ToString()
            && cart.TargetCashRegisterDeviceId == deviceId;

        if (!isOwnerDevice && !isTargetCashRegister)
            return NotFound();

        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var productIds = cart.Items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
        var productMap = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.ContainerTypeId, p.SaleType })
            .ToDictionaryAsync(p => p.Id);

        var responseItems = new List<CartContainerCheckItemResponse>();
        foreach (var item in cart.Items)
        {
            if (!item.ProductId.HasValue) continue;
            if (!productMap.TryGetValue(item.ProductId.Value, out var product)) continue;
            if (!product.ContainerTypeId.HasValue || product.SaleType != SaleType.Unit.ToString()) continue;

            var owedQty = Math.Max(0m, item.Quantity - item.ContainerReturnedNowQty);
            if (owedQty <= 0) continue;

            responseItems.Add(new CartContainerCheckItemResponse
            {
                ItemId = item.Id,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                ContainerReturnedNowQty = item.ContainerReturnedNowQty,
                OwedQty = owedQty
            });
        }

        return Ok(new CartContainerCheckResponse
        {
            CartId = cart.Id,
            HasOwedContainers = responseItems.Any(),
            Items = responseItems
        });
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult<CartItemResponse>> AddItem(int id, [FromBody] CartItemCreateRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var cart = await _cartService.GetByIdForDeviceAsync(id, deviceId);

        if (cart == null)
            return NotFound();

        if (cart.Status != CartStatus.Open.ToString())
            return BadRequest(new { message = "No se pueden agregar ítems a un carrito que no está abierto" });

        var productId = request.ProductId;
        var productCode = request.ProductCode;
        var productName = request.ProductName;
        var unitPrice = request.UnitPrice;
        var unit = request.Unit;

        if (!productId.HasValue && !string.IsNullOrWhiteSpace(request.ProductCode))
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<IProductLookupService>();
            var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
            var product = await resolver.ResolveByScannedCodeAsync(request.ProductCode);
            if (product != null)
            {
                productId = product.Id;
                productName = string.IsNullOrWhiteSpace(request.ProductName) ? product.Name : request.ProductName;
                productCode = request.ProductCode;
                unit = product.SaleType == SaleType.Weight.ToString() ? MeasureUnit.Weight.ToString() : MeasureUnit.Unit.ToString();

                if (unitPrice <= 0)
                {
                    var generalPrice = await db.ProductPrices
                        .Include(pp => pp.PriceList)
                        .Where(pp => pp.ProductId == product.Id && pp.PriceList.IsDefault)
                        .OrderByDescending(pp => pp.CreatedAt)
                        .FirstOrDefaultAsync();

                    unitPrice = product.SaleType == SaleType.Weight.ToString()
                        ? (generalPrice?.PricePerKg ?? product.DefaultPricePerKg)
                        : (generalPrice?.Price ?? product.DefaultPrice);
                }
            }
        }

        var item = await _cartService.AddItemAsync(
            id,
            productId,
            productCode,
            productName,
            unitPrice,
            request.Quantity,
            unit,
            request.Discount,
            request.ContainerReturnedNowQty
        );

        return Ok(ToCartItemResponse(item));
    }

    [HttpPut("{id}/items/{itemId}")]
    public async Task<ActionResult<CartItemResponse>> UpdateItem(int id, int itemId, [FromBody] CartItemUpdateRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var cart = await _cartService.GetByIdForDeviceAsync(id, deviceId);

        if (cart == null)
            return NotFound();

        if (cart.Status != CartStatus.Open.ToString())
            return BadRequest(new { message = "No se pueden editar ítems en un carrito que no está abierto" });

        var item = await _cartService.UpdateItemAsync(id, itemId, request.UnitPrice, request.Quantity, request.Discount, request.ContainerReturnedNowQty);

        if (item == null)
            return NotFound();

        return Ok(ToCartItemResponse(item));
    }

    [HttpDelete("{id}/items/{itemId}")]
    public async Task<ActionResult> RemoveItem(int id, int itemId)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var cart = await _cartService.GetByIdForDeviceAsync(id, deviceId);

        if (cart == null)
            return NotFound();

        if (cart.Status != CartStatus.Open.ToString())
            return BadRequest(new { message = "No se pueden quitar ítems de un carrito que no está abierto" });

        var removed = await _cartService.RemoveItemAsync(id, itemId);

        if (!removed)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/send-to-cashier")]
    [OperatorSessionAuth]
    public async Task<ActionResult<CartResponse>> SendToCashier(int id)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var sessionId = (int)HttpContext.Items["SessionId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var session = await dbContext.OperatorSessions.FindAsync(sessionId);
        var device = await dbContext.Devices.FindAsync(deviceId);
        
        if (session == null)
            return Unauthorized();

        var cart = await _cartService.GetByIdForDeviceAsync(id, deviceId);

        if (cart == null)
            return NotFound();

        if (cart.Status != CartStatus.Open.ToString())
            return BadRequest(new { message = "El carrito no está abierto" });

        int? targetCashRegisterDeviceId;
        if (device?.DeviceType == "CashRegister")
        {
            targetCashRegisterDeviceId = deviceId;
        }
        else if (device?.ParentCashRegisterDeviceId != null)
        {
            targetCashRegisterDeviceId = device.ParentCashRegisterDeviceId;
        }
        else
        {
            targetCashRegisterDeviceId = await dbContext.Devices
                .Where(d => d.StoreId == device!.StoreId && d.DeviceType == "CashRegister")
                .OrderBy(d => d.Id)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            if (!targetCashRegisterDeviceId.HasValue)
                return BadRequest(new { message = "El dispositivo no está asociado a una caja" });
        }

        await _cartService.SendToCashierAsync(id, sessionId, targetCashRegisterDeviceId);

        var updatedCart = await _cartService.GetByIdAsync(id);
        return Ok(ToCartResponse(updatedCart!));
    }

    private static CartResponse ToCartResponse(Cart cart)
    {
        return new CartResponse
        {
            Id = cart.Id,
            DeviceId = cart.DeviceId,
            Status = cart.Status,
            CreatedAt = cart.CreatedAt,
            SentToCashierAt = cart.SentToCashierAt,
            ConvertedAt = cart.ConvertedAt,
            Items = cart.Items.Select(ToCartItemResponse).ToList()
        };
    }

    private static CartItemResponse ToCartItemResponse(CartItem item)
    {
        return new CartItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Discount = item.Discount,
            ContainerReturnedNowQty = item.ContainerReturnedNowQty,
            Subtotal = item.Subtotal
        };
    }
}
