using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/purchases")]
[DeviceAuth]
[OperatorSessionAuth]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly IRequestDeduplicationService _requestDeduplication;

    public PurchasesController(IPurchaseService purchaseService, IRequestDeduplicationService requestDeduplication)
    {
        _purchaseService = purchaseService;
        _requestDeduplication = requestDeduplication;
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseResponse>> Create([FromBody] PurchaseCreateRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        var items = request.Items.Select(i => (i.ProductId, i.Quantity, i.UnitCost, i.ExpiryDate, i.DamagedForClaimQty, i.DiscardQty, i.UpdateSalePrice, i.NewSalePrice, i.NewPricePerKg)).ToList();

        var purchase = await _purchaseService.CreateAsync(
            usuarioId, deviceId, request.SupplierId, request.DocType, request.DocNumber, request.PurchaseDate, items
        );

        return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, ToResponse(purchase));
    }

    [HttpGet]
    public async Task<ActionResult<List<PurchaseResponse>>> GetAll([FromQuery] string? status = null)
    {
        var purchases = await _purchaseService.GetAllAsync(status);
        return Ok(purchases.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseResponse>> GetById(int id)
    {
        var purchase = await _purchaseService.GetByIdAsync(id);
        if (purchase == null) return NotFound();
        return Ok(ToResponse(purchase));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PurchaseResponse>> Update(int id, [FromBody] PurchaseUpdateRequest request)
    {
        try
        {
            List<(int, decimal, decimal, DateTime?, decimal, decimal, bool, decimal?, decimal?)>? items = null;
            if (request.Items != null)
            {
                items = request.Items.Select(i => (i.ProductId, i.Quantity, i.UnitCost, i.ExpiryDate, i.DamagedForClaimQty, i.DiscardQty, i.UpdateSalePrice, i.NewSalePrice, i.NewPricePerKg)).ToList();
            }

            var purchase = await _purchaseService.UpdateAsync(id, request.SupplierId, request.DocType, request.DocNumber, request.PurchaseDate, items);
            return Ok(ToResponse(purchase));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<PurchaseResponse>> Confirm(int id)
    {
        using var dedup = _requestDeduplication.Acquire($"purchases/{id}/confirm");
        var purchase = await _purchaseService.ConfirmAsync(id);
        return Ok(ToResponse(purchase));
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<PurchaseResponse>> Cancel(int id, [FromBody] PurchaseCancelRequest request)
    {
        try
        {
            var purchase = await _purchaseService.CancelAsync(id, request.Reason);
            return Ok(ToResponse(purchase));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static PurchaseResponse ToResponse(Purchase p)
    {
        return new PurchaseResponse
        {
            Id = p.Id,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.Name,
            DocType = p.DocType,
            DocNumber = p.DocNumber,
            PurchaseDate = p.PurchaseDate,
            Status = p.Status,
            Subtotal = p.Subtotal,
            Tax = p.Tax,
            Total = p.Total,
            CancelReason = p.CancelReason,
            CreatedAt = p.CreatedAt,
            ConfirmedAt = p.ConfirmedAt,
            CancelledAt = p.CancelledAt,
            Items = p.Items?.Select(ToItemResponse).ToList() ?? new()
        };
    }

    private static PurchaseItemResponse ToItemResponse(PurchaseItem item)
    {
        return new PurchaseItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? "",
            Quantity = item.Quantity,
            UnitCost = item.UnitCost,
            ExpiryDate = item.ExpiryDate,
            DamagedForClaimQty = item.DamagedForClaimQty,
            DiscardQty = item.DiscardQty,
            VendibleQty = item.VendibleQty,
            UpdateSalePrice = item.UpdateSalePrice,
            NewSalePrice = item.NewSalePrice,
            NewPricePerKg = item.NewPricePerKg
        };
    }
}

[ApiController]
[Route("api/v1/suppliers")]
[OperatorSessionAuth]
public class SuppliersController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public SuppliersController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpPost]
    public async Task<ActionResult<SupplierResponse>> Create([FromBody] SupplierCreateRequest request)
    {
        try
        {
            var supplier = await _purchaseService.CreateSupplierAsync(
                request.Name,
                request.CUIT,
                request.Address,
                request.Phone,
                request.Email,
                request.ClaimSettlementModeDefault,
                request.AllowClaimSettlementOverride
            );
            return CreatedAtAction(nameof(GetAll), new { id = supplier.Id }, ToResponse(supplier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{supplierId}")]
    public async Task<ActionResult<SupplierResponse>> Update(int supplierId, [FromBody] SupplierUpdateRequest request)
    {
        try
        {
            var supplier = await _purchaseService.UpdateSupplierAsync(
                supplierId,
                request.Name,
                request.CUIT,
                request.Address,
                request.Phone,
                request.Email,
                request.IsActive,
                request.ClaimSettlementModeDefault,
                request.AllowClaimSettlementOverride
            );
            return Ok(ToResponse(supplier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<SupplierResponse>>> GetAll()
    {
        var suppliers = await _purchaseService.GetSuppliersAsync();
        return Ok(suppliers.Select(ToResponse).ToList());
    }

    [HttpPost("{supplierId}/returns")]
    [DeviceAuth]
    public async Task<ActionResult<SupplierReturnResponse>> CreateReturn(int supplierId, [FromBody] SupplierReturnCreateRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        try
        {
            var lines = request.Lines.Select(l => (l.ProductId, l.Qty)).ToList();
            var entity = await _purchaseService.CreateSupplierReturnAsync(supplierId, deviceId, operatorSessionId, usuarioId, request.Date, lines);
            return Ok(ToSupplierReturnResponse(entity));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{supplierId}/exchanges")]
    [DeviceAuth]
    public async Task<ActionResult<ExternalExchangeResponse>> CreateExchange(int supplierId, [FromBody] ExternalExchangeCreateRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        try
        {
            var lines = request.Lines.Select(l => (l.Direction, l.ProductId, l.Qty)).ToList();
            var entity = await _purchaseService.CreateExternalExchangeAsync(supplierId, deviceId, operatorSessionId, usuarioId, request.Date, lines);
            return Ok(ToExternalExchangeResponse(entity));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static SupplierResponse ToResponse(Supplier s)
    {
        return new SupplierResponse
        {
            Id = s.Id,
            Name = s.Name,
            CUIT = s.CUIT,
            Address = s.Address,
            Phone = s.Phone,
            Email = s.Email,
            IsActive = s.IsActive,
            ClaimSettlementModeDefault = s.ClaimSettlementModeDefault,
            AllowClaimSettlementOverride = s.AllowClaimSettlementOverride,
            CreatedAt = s.CreatedAt
        };
    }

    private static SupplierReturnResponse ToSupplierReturnResponse(SupplierReturn r)
    {
        return new SupplierReturnResponse
        {
            Id = r.Id,
            SupplierId = r.SupplierId,
            ReturnDate = r.ReturnDate,
            CreatedAt = r.CreatedAt,
            Lines = r.Lines.Select(l => new SupplierReturnLineResponse
            {
                Id = l.Id,
                ProductId = l.ProductId,
                Qty = l.Qty,
                UnitCostSnapshot = l.UnitCostSnapshot
            }).ToList()
        };
    }

    private static ExternalExchangeResponse ToExternalExchangeResponse(ExternalExchange e)
    {
        return new ExternalExchangeResponse
        {
            Id = e.Id,
            SupplierId = e.SupplierId,
            ExchangeDate = e.ExchangeDate,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.Select(l => new ExternalExchangeLineResponse
            {
                Id = l.Id,
                Direction = l.Direction,
                ProductId = l.ProductId,
                Qty = l.Qty
            }).ToList()
        };
    }
}
