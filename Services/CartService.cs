using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface ICartService
{
    Task<Cart> CreateAsync(int deviceId);
    Task<Cart?> GetByIdAsync(int id);
    Task<Cart?> GetByIdForDeviceAsync(int id, int deviceId);
    Task<CartItem> AddItemAsync(int cartId, int? productId, string productCode, string productName, decimal unitPrice, decimal quantity, string unit, decimal discount, decimal containerReturnedNowQty);
    Task<CartItem?> UpdateItemAsync(int cartId, int itemId, decimal? unitPrice, decimal? quantity, decimal? discount, decimal? containerReturnedNowQty);
    Task<bool> RemoveItemAsync(int cartId, int itemId);
    Task<Cart> SendToCashierAsync(int cartId, int operatorSessionId, int? targetCashRegisterDeviceId);
    Task<List<Cart>> GetSentToCashierAsync();
    Task<List<Cart>> GetSentToCashierForDeviceAsync(int deviceId);
    Task<List<Cart>> GetSentToCashierForCashRegisterAsync(int cashRegisterDeviceId);
}

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart> CreateAsync(int deviceId)
    {
        var openCart = await _context.Carts
            .FirstOrDefaultAsync(c => c.DeviceId == deviceId && c.Status == CartStatus.Open.ToString());

        if (openCart != null)
            return openCart;

        var storeId = await _context.Devices
            .Where(d => d.Id == deviceId)
            .Select(d => d.StoreId)
            .FirstOrDefaultAsync();

        var cart = new Cart
        {
            DeviceId = deviceId,
            StoreId = storeId,
            Status = CartStatus.Open.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return cart;
    }

    public async Task<Cart?> GetByIdAsync(int id)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cart?> GetByIdForDeviceAsync(int id, int deviceId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeviceId == deviceId);
    }

    public async Task<CartItem> AddItemAsync(int cartId, int? productId, string productCode, string productName, decimal unitPrice, decimal quantity, string unit, decimal discount, decimal containerReturnedNowQty)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
            throw new InvalidOperationException("Carrito no encontrado");

        if (cart.Status != CartStatus.Open.ToString())
            throw new InvalidOperationException("El carrito no está abierto");

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductCode == productCode);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            existingItem.UnitPrice = unitPrice;
            existingItem.ContainerReturnedNowQty += containerReturnedNowQty;
            await _context.SaveChangesAsync();
            return existingItem;
        }

        var item = new CartItem
        {
            CartId = cartId,
            ProductId = productId,
            ProductCode = productCode,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            Unit = unit,
            Discount = discount,
            ContainerReturnedNowQty = containerReturnedNowQty,
            CreatedAt = DateTime.UtcNow
        };

        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<CartItem?> UpdateItemAsync(int cartId, int itemId, decimal? unitPrice, decimal? quantity, decimal? discount, decimal? containerReturnedNowQty)
    {
        var item = await _context.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cartId);

        if (item == null)
            return null;

        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart == null || cart.Status != CartStatus.Open.ToString())
            throw new InvalidOperationException("El carrito no está abierto");

        if (unitPrice.HasValue) item.UnitPrice = unitPrice.Value;
        if (quantity.HasValue) item.Quantity = quantity.Value;
        if (discount.HasValue) item.Discount = discount.Value;
        if (containerReturnedNowQty.HasValue) item.ContainerReturnedNowQty = containerReturnedNowQty.Value;

        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> RemoveItemAsync(int cartId, int itemId)
    {
        var item = await _context.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cartId);

        if (item == null)
            return false;

        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart == null || cart.Status != CartStatus.Open.ToString())
            throw new InvalidOperationException("El carrito no está abierto");

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Cart> SendToCashierAsync(int cartId, int operatorSessionId, int? targetCashRegisterDeviceId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
            throw new InvalidOperationException("Carrito no encontrado");

        if (cart.Status != CartStatus.Open.ToString())
            throw new InvalidOperationException("El carrito no está abierto");

        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty");

        cart.Status = CartStatus.SentToCashier.ToString();
        cart.SentToCashierAt = DateTime.UtcNow;
        cart.OperatorSessionId = operatorSessionId;
        cart.TargetCashRegisterDeviceId = targetCashRegisterDeviceId;

        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task<List<Cart>> GetSentToCashierAsync()
    {
        return await _context.Carts
            .Include(c => c.Items)
            .Where(c => c.Status == CartStatus.SentToCashier.ToString())
            .OrderBy(c => c.SentToCashierAt)
            .ToListAsync();
    }

    public async Task<List<Cart>> GetSentToCashierForDeviceAsync(int deviceId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .Where(c => c.DeviceId == deviceId && c.Status == CartStatus.SentToCashier.ToString())
            .OrderBy(c => c.SentToCashierAt)
            .ToListAsync();
    }

    public async Task<List<Cart>> GetSentToCashierForCashRegisterAsync(int cashRegisterDeviceId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .Where(c => c.TargetCashRegisterDeviceId == cashRegisterDeviceId && c.Status == CartStatus.SentToCashier.ToString())
            .OrderBy(c => c.SentToCashierAt)
            .ToListAsync();
    }
}
