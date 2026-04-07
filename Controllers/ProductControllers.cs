using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/products")]
[DeviceAuth]
[OperatorSessionAuth]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductCreateRequest request)
    {
        var product = await _productService.CreateAsync(
            request.Name, request.Barcode, request.QuickCode, request.SaleType,
            request.IsCigarette, request.AllowsManualPrice, request.TracksExpiry,
            request.StockControl, request.ContainerTypeId, request.ContainerDepositOverride,
            request.UnitName, request.DefaultPrice, request.DefaultPricePerKg
        );
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> GetAll([FromQuery] string? status = null)
    {
        var products = await _productService.GetAllAsync(status);
        return Ok(products.Select(ToResponse).ToList());
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<ProductResponse>>> GetPending()
    {
        if (!await IsAdminOrSupervisorAsync()) return Forbid();
        var products = await _productService.GetPendingAsync();
        return Ok(products.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(ToResponse(product));
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductResponse>> GetByBarcode(string barcode)
    {
        var product = await _productService.GetByBarcodeAsync(barcode);
        if (product == null) return NotFound();
        return Ok(ToResponse(product));
    }

    [HttpGet("quickcode/{quickCode}")]
    public async Task<ActionResult<ProductResponse>> GetByQuickCode(string quickCode)
    {
        var product = await _productService.GetByQuickCodeAsync(quickCode);
        if (product == null) return NotFound();
        return Ok(ToResponse(product));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, [FromBody] ProductUpdateRequest request)
    {
        if (!await IsAdminOrSupervisorAsync()) return Forbid();
        try
        {
            var product = await _productService.UpdateAsync(
                id, request.Name, request.Barcode, request.QuickCode, request.SaleType,
                request.IsCigarette, request.AllowsManualPrice, request.TracksExpiry,
                request.StockControl, request.ContainerTypeId, request.ContainerDepositOverride,
                request.CatalogStatus, request.UnitName,
                request.DefaultPrice, request.DefaultPricePerKg
            );
            return Ok(ToResponse(product));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<ProductResponse>> Activate(int id)
    {
        if (!await IsAdminOrSupervisorAsync()) return Forbid();
        try
        {
            var product = await _productService.ActivateAsync(id);
            return Ok(ToResponse(product));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Barcode = p.Barcode, QuickCode = p.QuickCode,
        SaleType = p.SaleType, IsCigarette = p.IsCigarette, AllowsManualPrice = p.AllowsManualPrice,
        TracksExpiry = p.TracksExpiry, StockControl = p.StockControl, CatalogStatus = p.CatalogStatus,
        ContainerTypeId = p.ContainerTypeId, ContainerDepositOverride = p.ContainerDepositOverride,
        UnitName = p.UnitName, DefaultPrice = p.DefaultPrice, DefaultPricePerKg = p.DefaultPricePerKg,
        CreatedAt = p.CreatedAt
    };

    private async Task<bool> IsAdminOrSupervisorAsync()
    {
        var userId = HttpContext.Items.TryGetValue("SessionUsuarioId", out var value) && value is int id ? id : (int?)null;
        if (!userId.HasValue) return false;

        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var usuario = await db.Usuarios.FindAsync(userId.Value);
        if (usuario == null) return false;

        return usuario.Role == UserRole.Admin.ToString() || usuario.Role == UserRole.Supervisor.ToString();
    }
}

[ApiController]
[Route("api/v1/price-lists")]
[DeviceAuth]
[OperatorSessionAuth]
public class PriceListsController : ControllerBase
{
    private readonly IPriceListService _priceListService;

    public PriceListsController(IPriceListService priceListService)
    {
        _priceListService = priceListService;
    }

    [HttpPost]
    public async Task<ActionResult<PriceListResponse>> Create([FromBody] PriceListCreateRequest request)
    {
        var list = await _priceListService.CreateAsync(request.Name, request.IsDefault, request.IsActive);
        return CreatedAtAction(nameof(GetById), new { id = list.Id }, ToResponse(list));
    }

    [HttpGet]
    public async Task<ActionResult<List<PriceListResponse>>> GetAll()
    {
        var lists = await _priceListService.GetAllAsync();
        return Ok(lists.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PriceListResponse>> GetById(int id)
    {
        var list = await _priceListService.GetByIdAsync(id);
        if (list == null) return NotFound();
        return Ok(ToResponse(list));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PriceListResponse>> Update(int id, [FromBody] PriceListUpdateRequest request)
    {
        try
        {
            var list = await _priceListService.UpdateAsync(id, request.Name, request.IsDefault, request.IsActive);
            return Ok(ToResponse(list));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("bulk-prices")]
    public async Task<ActionResult> BulkUpdatePrices([FromBody] BulkPriceUpdateRequest request)
    {
        var prices = request.Prices.Select(p => (p.ProductId, p.Price, p.PricePerKg)).ToList();
        await _priceListService.BulkUpdatePricesAsync(request.PriceListId, prices);
        return Ok(new { message = "Prices updated successfully" });
    }

    private static PriceListResponse ToResponse(PriceList pl) => new()
    {
        Id = pl.Id, Name = pl.Name, IsDefault = pl.IsDefault, IsActive = pl.IsActive, CreatedAt = pl.CreatedAt
    };
}

[ApiController]
[Route("api/v1/promotions")]
[DeviceAuth]
[OperatorSessionAuth]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionsController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpPost]
    public async Task<ActionResult<PromotionResponse>> Create([FromBody] PromotionCreateRequest request)
    {
        try
        {
            var promotion = await _promotionService.CreateAsync(
                request.Name, request.PromotionType, request.NxM_BuyQuantity, request.NxM_FreeQuantity,
                request.PercentDiscount, request.PackPrice, request.MinPurchaseAmount, request.Priority,
                request.StartDate, request.EndDate, request.IsActive, request.ProductIds
            );
            return CreatedAtAction(nameof(GetById), new { id = promotion.Id }, ToResponse(promotion));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<PromotionResponse>>> GetAll()
    {
        var promotions = await _promotionService.GetAllAsync();
        return Ok(promotions.Select(ToResponse).ToList());
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<PromotionResponse>>> GetActive()
    {
        var promotions = await _promotionService.GetActiveAsync();
        return Ok(promotions.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PromotionResponse>> GetById(int id)
    {
        var promotion = await _promotionService.GetByIdAsync(id);
        if (promotion == null) return NotFound();
        return Ok(ToResponse(promotion));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PromotionResponse>> Update(int id, [FromBody] PromotionUpdateRequest request)
    {
        try
        {
            var promotion = await _promotionService.UpdateAsync(
                id, request.Name, request.PromotionType, request.NxM_BuyQuantity, request.NxM_FreeQuantity,
                request.PercentDiscount, request.PackPrice, request.MinPurchaseAmount, request.Priority,
                request.StartDate, request.EndDate, request.IsActive, request.ProductIds
            );
            return Ok(ToResponse(promotion));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static PromotionResponse ToResponse(Promotion p) => new()
    {
        Id = p.Id, Name = p.Name, PromotionType = p.PromotionType,
        NxM_BuyQuantity = p.NxM_BuyQuantity, NxM_FreeQuantity = p.NxM_FreeQuantity,
        PercentDiscount = p.PercentDiscount, PackPrice = p.PackPrice,
        MinPurchaseAmount = p.MinPurchaseAmount, Priority = p.Priority,
        StartDate = p.StartDate, EndDate = p.EndDate, IsActive = p.IsActive,
        ProductIds = p.Products.Select(pp => pp.ProductId).ToList(), CreatedAt = p.CreatedAt
    };
}

[ApiController]
[Route("api/v1/settings")]
[DeviceAuth]
[OperatorSessionAuth]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("pos")]
    public async Task<ActionResult<Dictionary<string, string>>> GetPOSSettings()
    {
        var settings = await _settingsService.GetPOSSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("pos")]
    public async Task<ActionResult> UpdatePOSSettings([FromBody] POSSettingsRequest request)
    {
        await _settingsService.UpdatePOSSettingsAsync(
            request.BigPurchaseMinAmount, request.BigPurchaseDiscountCapPercent,
            request.CigaretteSurchargePercent, request.CigaretteSurchargeMethods,
            request.LateFeeEnabled, request.LateFeePercentMonthly
        );
        return Ok(new { message = "Settings updated successfully" });
    }
}

[ApiController]
[Route("api/v1/container-types")]
[DeviceAuth]
[OperatorSessionAuth]
public class ContainerTypesController : ControllerBase
{
    private readonly Data.ApplicationDbContext _context;

    public ContainerTypesController(Data.ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ContainerTypeResponse>>> GetAll()
    {
        var types = await _context.ContainerTypes.OrderBy(t => t.Name).ToListAsync();
        return Ok(types.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ContainerTypeResponse>> Create([FromBody] ContainerTypeCreateRequest request)
    {
        if (!await IsManagerOrAdminAsync())
            return Forbid();

        var type = new ContainerType
        {
            Name = request.Name,
            DepositAmount = request.DepositAmount,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.ContainerTypes.Add(type);
        await _context.SaveChangesAsync();
        return Ok(ToResponse(type));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ContainerTypeResponse>> Update(int id, [FromBody] ContainerTypeUpdateRequest request)
    {
        if (!await IsManagerOrAdminAsync())
            return Forbid();

        var type = await _context.ContainerTypes.FindAsync(id);
        if (type == null)
            return NotFound();

        if (request.Name != null) type.Name = request.Name;
        if (request.DepositAmount.HasValue) type.DepositAmount = request.DepositAmount.Value;
        if (request.IsActive.HasValue) type.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();
        return Ok(ToResponse(type));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await IsManagerOrAdminAsync())
            return Forbid();

        var type = await _context.ContainerTypes.FindAsync(id);
        if (type == null)
            return NotFound();

        _context.ContainerTypes.Remove(type);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> IsManagerOrAdminAsync()
    {
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var usuario = await _context.Usuarios.FindAsync(sessionUsuarioId);
        return usuario?.Role == UserRole.Admin.ToString() || usuario?.Role == UserRole.Supervisor.ToString();
    }

    private static ContainerTypeResponse ToResponse(ContainerType type)
    {
        return new ContainerTypeResponse
        {
            Id = type.Id,
            Name = type.Name,
            DepositAmount = type.DepositAmount,
            IsActive = type.IsActive,
            CreatedAt = type.CreatedAt
        };
    }
}
