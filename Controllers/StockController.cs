using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;
using System.Text.Json;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/stock")]
[OperatorSessionAuth]
public class StockController : ControllerBase
{
    private const string TransformationTemplatesSettingKey = "StockTransformationTemplates";
    private const string TransformationYieldPolicySettingKey = "StockTransformationYieldPolicy";
    private const int MaxClaimEvidenceFiles = 5;
    private const long MaxClaimEvidenceFileSize = 6 * 1024 * 1024;
    private static readonly string[] ClaimEvidenceContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
    private readonly IStockService _stockService;
    private readonly ApplicationDbContext _context;
    private readonly IRequestDeduplicationService _requestDeduplication;

    public StockController(IStockService stockService, ApplicationDbContext context, IRequestDeduplicationService requestDeduplication)
    {
        _stockService = stockService;
        _context = context;
        _requestDeduplication = requestDeduplication;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductStockDto>>> GetAllStock()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        var stocks = await _stockService.GetAllStockAsync();
        if (activeStoreId.HasValue)
            stocks = stocks.Where(s => _context.ProductStocks.Any(ps => ps.Id == s.Id && ps.StoreId == activeStoreId.Value)).ToList();
        return Ok(stocks);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<decimal>> GetBalance([FromQuery] int productId, [FromQuery] string? bucket = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var balance = await _stockService.GetStockBalanceAsync(productId, bucket);
        return Ok(balance);
    }

    [HttpGet("report")]
    public async Task<ActionResult<StockReportDto>> GetReport()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        var balances = await _stockService.GetStockReportAsync(activeStoreId);
        return Ok(new StockReportDto
        {
            Balances = balances,
            GeneratedAt = DateTime.UtcNow
        });
    }

    [HttpGet("movements")]
    public async Task<ActionResult<List<StockMovementDto>>> GetMovements(
        [FromQuery] int? productId = null,
        [FromQuery] string? movementType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        var startUtc = ToUtcOrNull(from?.Date);
        var endUtc = ToUtcOrNull(to?.Date.AddDays(1).AddTicks(-1));
        var movements = await _stockService.GetMovementsAsync(productId, movementType, startUtc, endUtc, activeStoreId);
        return Ok(movements);
    }

    [HttpPost("movement")]
    public async Task<ActionResult<StockMovementDto>> CreateMovement([FromBody] CreateStockMovementDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var deviceId = (int?)HttpContext.Items["DeviceId"];
        var operatorSessionId = (int?)HttpContext.Items["SessionId"];
        var storeId = GetActiveStoreId() ?? (int?)HttpContext.Items["StoreId"];

        var movement = await _stockService.ApplyMovementAsync(
            request.ProductId,
            request.Bucket,
            request.DeltaQty,
            request.MovementType,
            request.PurchaseId,
            request.SaleId,
            request.SupplierClaimId,
            operatorSessionId,
            deviceId,
            request.Notes,
            storeId
        );

        return Ok(new StockMovementDto
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            Bucket = movement.Bucket,
            DeltaQty = movement.DeltaQty,
            MovementType = movement.MovementType,
            PurchaseId = movement.PurchaseId,
            SaleId = movement.SaleId,
            SupplierClaimId = movement.SupplierClaimId,
            OperatorSessionId = movement.OperatorSessionId,
            DeviceId = movement.DeviceId,
            Notes = movement.Notes,
            CreatedAt = movement.CreatedAt
        });
    }

    [HttpGet("claims")]
    public async Task<ActionResult<List<SupplierClaimDto>>> GetSupplierClaims([FromQuery] string? status = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        var claims = await _stockService.GetSupplierClaimsAsync(status);
        if (activeStoreId.HasValue)
            claims = claims.Where(c => _context.SupplierClaims.Any(sc => sc.Id == c.Id && sc.StoreId == activeStoreId.Value)).ToList();
        return Ok(claims);
    }

    [HttpGet("claims/{id}")]
    public async Task<ActionResult<SupplierClaimDto>> GetSupplierClaimById(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        var claim = await _stockService.GetSupplierClaimByIdAsync(id);
        if (claim == null) return NotFound();
        if (activeStoreId.HasValue && claim.StoreId != activeStoreId.Value) return NotFound();

        return Ok(BuildSupplierClaimDto(claim));
    }

    [HttpPost("claims")]
    public async Task<ActionResult<SupplierClaimDto>> CreateSupplierClaim([FromBody] CreateSupplierClaimDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var storeId = GetActiveStoreId() ?? (int?)HttpContext.Items["StoreId"];
        var items = request.Items.Select(i => (i.ProductId, i.Quantity, i.UnitCostSnapshot, i.Notes)).ToList();
        var claim = await _stockService.CreateSupplierClaimAsync(
            request.SupplierId,
            request.PurchaseId,
            request.HasReceipt,
            request.ReceiptType,
            request.ReceiptNumber,
            request.Notes,
            items,
            evidences: null,
            settlementMode: request.SettlementMode,
            resolvedByUserId: (int?)HttpContext.Items["SessionUsuarioId"]
        );

        if (storeId.HasValue)
        {
            claim.StoreId = storeId.Value;
            await _context.SaveChangesAsync();
        }

        var fullClaim = await _stockService.GetSupplierClaimByIdAsync(claim.Id);
        if (fullClaim == null)
            return CreatedAtAction(nameof(GetSupplierClaimById), new { id = claim.Id }, BuildSupplierClaimDto(claim));

        return CreatedAtAction(nameof(GetSupplierClaimById), new { id = fullClaim.Id }, BuildSupplierClaimDto(fullClaim));
    }

    [HttpPost("claims/with-evidence")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<SupplierClaimDto>> CreateSupplierClaimWithEvidence([FromForm] CreateSupplierClaimWithEvidenceRequest request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var storeId = GetActiveStoreId() ?? (int?)HttpContext.Items["StoreId"];

        List<CreateSupplierClaimItemDto>? itemsDto;
        try
        {
            itemsDto = JsonSerializer.Deserialize<List<CreateSupplierClaimItemDto>>(request.ItemsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return BadRequest(new { message = "El formato de items no es válido." });
        }

        if (itemsDto == null || itemsDto.Count == 0)
            return BadRequest(new { message = "Debe enviar al menos un producto para el reclamo." });

        var validItems = itemsDto
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .Select(i => (i.ProductId, i.Quantity, i.UnitCostSnapshot, i.Notes))
            .ToList();

        if (validItems.Count == 0)
            return BadRequest(new { message = "Los productos enviados no son válidos." });

        if (request.EvidenceFiles != null && request.EvidenceFiles.Count > MaxClaimEvidenceFiles)
            return BadRequest(new { message = $"Se permiten hasta {MaxClaimEvidenceFiles} fotos por reclamo." });

        var evidences = new List<(string fileName, string contentType, long fileSize, byte[] fileContent)>();
        if (request.EvidenceFiles != null)
        {
            foreach (var file in request.EvidenceFiles)
            {
                if (file == null || file.Length <= 0) continue;
                if (file.Length > MaxClaimEvidenceFileSize)
                    return BadRequest(new { message = $"El archivo {file.FileName} supera el tamaño máximo de 6 MB." });

                var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
                if (!ClaimEvidenceContentTypes.Contains(contentType))
                    return BadRequest(new { message = $"El archivo {file.FileName} no tiene un formato permitido (jpg, png, webp)." });

                await using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                evidences.Add((
                    string.IsNullOrWhiteSpace(file.FileName) ? "evidencia" : Path.GetFileName(file.FileName),
                    contentType,
                    file.Length,
                    ms.ToArray()
                ));
            }
        }

        var claim = await _stockService.CreateSupplierClaimAsync(
            request.SupplierId,
            request.PurchaseId,
            request.HasReceipt,
            request.ReceiptType,
            request.ReceiptNumber,
            request.Notes,
            validItems,
            evidences,
            settlementMode: request.SettlementMode,
            resolvedByUserId: (int?)HttpContext.Items["SessionUsuarioId"]
        );

        if (storeId.HasValue)
        {
            claim.StoreId = storeId.Value;
            await _context.SaveChangesAsync();
        }

        var fullClaim = await _stockService.GetSupplierClaimByIdAsync(claim.Id);
        if (fullClaim == null)
            return CreatedAtAction(nameof(GetSupplierClaimById), new { id = claim.Id }, BuildSupplierClaimDto(claim));

        return CreatedAtAction(nameof(GetSupplierClaimById), new { id = fullClaim.Id }, BuildSupplierClaimDto(fullClaim));
    }

    [HttpGet("claims/{claimId}/evidences/{evidenceId}/download")]
    public async Task<ActionResult> DownloadClaimEvidence(int claimId, int evidenceId)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var activeStoreId = GetActiveStoreId();
        var evidence = await _context.SupplierClaimEvidences
            .Include(e => e.SupplierClaim)
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.SupplierClaimId == claimId);

        if (evidence == null) return NotFound();
        if (activeStoreId.HasValue && evidence.SupplierClaim?.StoreId != activeStoreId.Value) return NotFound();

        return File(evidence.FileContent, evidence.ContentType, evidence.FileName);
    }

    [HttpGet("claims/{claimId}/evidences/{evidenceId}/preview")]
    public async Task<ActionResult> PreviewClaimEvidence(int claimId, int evidenceId)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var activeStoreId = GetActiveStoreId();
        var evidence = await _context.SupplierClaimEvidences
            .Include(e => e.SupplierClaim)
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.SupplierClaimId == claimId);

        if (evidence == null) return NotFound();
        if (activeStoreId.HasValue && evidence.SupplierClaim?.StoreId != activeStoreId.Value) return NotFound();

        return File(evidence.FileContent, evidence.ContentType);
    }

    [HttpPost("claims/{id}/pickup")]
    public async Task<ActionResult<SupplierClaimDto>> PickUpClaim(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        using var dedup = _requestDeduplication.Acquire($"stock/claims/{id}/pickup");
        SupplierClaim claim;
        try
        {
            claim = await _stockService.PickUpSupplierClaimAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return Ok(new SupplierClaimDto
        {
            Id = claim.Id,
            Status = claim.Status,
            PickedUpAt = claim.PickedUpAt
        });
    }

    [HttpPost("claims/{id}/credit")]
    public async Task<ActionResult<SupplierClaimDto>> CreditClaim(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        using var dedup = _requestDeduplication.Acquire($"stock/claims/{id}/credit");
        SupplierClaim claim;
        try
        {
            claim = await _stockService.CreditSupplierClaimAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return Ok(new SupplierClaimDto
        {
            Id = claim.Id,
            Status = claim.Status,
            CreditedAt = claim.CreditedAt
        });
    }

    [HttpPost("claims/{id}/resolve/credit")]
    public Task<ActionResult<SupplierClaimDto>> ResolveClaimCredit(int id)
        => CreditClaim(id);

    [HttpPost("claims/{id}/resolve/refund")]
    public async Task<ActionResult<SupplierClaimDto>> ResolveClaimRefund(int id, [FromBody] ResolveSupplierClaimRefundDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        using var dedup = _requestDeduplication.Acquire($"stock/claims/{id}/refund/{request.Amount:0.##}");
        SupplierClaim claim;
        try
        {
            claim = await _stockService.RefundSupplierClaimAsync(id, request.Amount, request.Notes, (int?)HttpContext.Items["SessionUsuarioId"]);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new SupplierClaimDto
        {
            Id = claim.Id,
            Status = claim.Status,
            ResolvedSettlementMode = claim.ResolvedSettlementMode,
            ResolvedAt = claim.ResolvedAt,
            CreditedAt = claim.CreditedAt
        });
    }

    [HttpPost("claims/{id}/resolve/exchange")]
    public async Task<ActionResult<SupplierClaimDto>> ResolveClaimExchange(int id, [FromBody] ResolveSupplierClaimExchangeDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        using var dedup = _requestDeduplication.Acquire($"stock/claims/{id}/exchange");
        SupplierClaim claim;
        try
        {
            var lines = request.Lines
                .Where(l => l.ProductId > 0 && l.Quantity > 0)
                .Select(l => (l.ProductId, l.Quantity, l.UnitCostSnapshot, l.Notes))
                .ToList();
            claim = await _stockService.ResolveSupplierClaimExchangeAsync(id, lines, request.Notes, (int?)HttpContext.Items["SessionUsuarioId"]);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new SupplierClaimDto
        {
            Id = claim.Id,
            Status = claim.Status,
            ResolvedSettlementMode = claim.ResolvedSettlementMode,
            ResolvedAt = claim.ResolvedAt,
            CreditedAt = claim.CreditedAt
        });
    }

    [HttpPost("claims/{id}/replace")]
    public async Task<ActionResult<SupplierClaimDto>> ReplaceClaim(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        using var dedup = _requestDeduplication.Acquire($"stock/claims/{id}/replace");
        SupplierClaim claim;
        try
        {
            claim = await _stockService.ReplaceSupplierClaimAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new SupplierClaimDto
        {
            Id = claim.Id,
            Status = claim.Status,
            CreditedAt = claim.CreditedAt,
            ResolvedSettlementMode = claim.ResolvedSettlementMode,
            ResolvedAt = claim.ResolvedAt
        });
    }

    [HttpPost("claims/{id}/refund")]
    public Task<ActionResult<SupplierClaimDto>> RefundClaim(int id, [FromBody] ResolveSupplierClaimRefundDto request)
        => ResolveClaimRefund(id, request);

    [HttpGet("credits")]
    public async Task<ActionResult<List<SupplierCreditDto>>> GetSupplierCredits([FromQuery] int? supplierId = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var credits = await _stockService.GetSupplierCreditsAsync(supplierId);
        return Ok(credits);
    }

    [HttpGet("credits/{id}")]
    public async Task<ActionResult<SupplierCreditDto>> GetSupplierCreditById(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var credit = await _stockService.GetSupplierCreditByIdAsync(id);
        if (credit == null) return NotFound();

        var applications = await _context.SupplierCreditApplications
            .Include(a => a.Purchase)
            .Where(a => a.SupplierCreditId == id)
            .ToListAsync();

        return Ok(new SupplierCreditDto
        {
            Id = credit.Id,
            SupplierId = credit.SupplierId,
            SupplierName = credit.Supplier?.Name,
            Amount = credit.Amount,
            RemainingAmount = credit.RemainingAmount,
            SupplierClaimId = credit.SupplierClaimId,
            Notes = credit.Notes,
            CreatedAt = credit.CreatedAt,
            Applications = applications.Select(a => new SupplierCreditApplicationDto
            {
                Id = a.Id,
                PurchaseId = a.PurchaseId,
                PurchaseNumber = a.Purchase?.DocNumber,
                AppliedAmount = a.AppliedAmount,
                CreatedAt = a.CreatedAt
            }).ToList()
        });
    }

    [HttpPost("credits/apply")]
    public async Task<ActionResult<SupplierCreditDto>> ApplyCredit([FromBody] CreateSupplierCreditApplicationDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        using var dedup = _requestDeduplication.Acquire($"stock/credits/{request.SupplierCreditId}/apply/{request.PurchaseId}");
        var credit = await _stockService.ApplySupplierCreditAsync(request.SupplierCreditId, request.PurchaseId, request.AppliedAmount);
        return Ok(new SupplierCreditDto
        {
            Id = credit.Id,
            RemainingAmount = credit.RemainingAmount
        });
    }

    [HttpGet("transformations/templates")]
    public async Task<ActionResult<List<StockTransformationTemplateDto>>> GetTransformationTemplates()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var templates = await GetTransformationTemplatesInternalAsync();
        var dto = templates
            .OrderByDescending(t => t.UpdatedAt)
            .Select(ToTransformationTemplateDto)
            .ToList();

        return Ok(dto);
    }

    [HttpPost("transformations/templates")]
    public async Task<ActionResult<StockTransformationTemplateDto>> UpsertTransformationTemplate([FromBody] UpsertStockTransformationTemplateDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        if (request.YieldFactor <= 0) return BadRequest(new { message = "YieldFactor debe ser mayor a 0" });
        if (request.SourceProductId == request.TargetProductId) return BadRequest(new { message = "Producto origen y destino deben ser distintos" });

        var templates = await GetTransformationTemplatesInternalAsync();
        var existing = templates.FirstOrDefault(t =>
            t.SupplierId == request.SupplierId
            && t.SourceProductId == request.SourceProductId
            && t.TargetProductId == request.TargetProductId);

        if (existing == null)
        {
            existing = new StockTransformationTemplateData();
            templates.Add(existing);
        }

        existing.SupplierId = request.SupplierId;
        existing.SourceProductId = request.SourceProductId;
        existing.TargetProductId = request.TargetProductId;
        existing.YieldFactor = request.YieldFactor;
        existing.Notes = request.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await SaveTransformationTemplatesInternalAsync(templates);
        return Ok(ToTransformationTemplateDto(existing));
    }

    [HttpPost("transformations/apply")]
    public async Task<ActionResult<object>> ApplyTransformation([FromBody] ApplyStockTransformationDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        if (request.SourceQty <= 0) return BadRequest(new { message = "SourceQty debe ser mayor a 0" });
        if (request.SourceProductId == request.TargetProductId) return BadRequest(new { message = "Producto origen y destino deben ser distintos" });

        var templates = await GetTransformationTemplatesInternalAsync();
        var template = templates.FirstOrDefault(t =>
            t.SupplierId == request.SupplierId
            && t.SourceProductId == request.SourceProductId
            && t.TargetProductId == request.TargetProductId);

        var yieldFactor = request.YieldFactor;
        if (!yieldFactor.HasValue && request.TargetQty.HasValue && request.SourceQty > 0)
        {
            yieldFactor = Math.Round(request.TargetQty.Value / request.SourceQty, 6, MidpointRounding.AwayFromZero);
        }

        yieldFactor ??= template?.YieldFactor ?? 1m;

        if (yieldFactor <= 0) return BadRequest(new { message = "YieldFactor debe ser mayor a 0" });

        var targetQty = request.TargetQty ?? Math.Round(request.SourceQty * yieldFactor.Value, 3, MidpointRounding.AwayFromZero);
        if (targetQty <= 0) return BadRequest(new { message = "TargetQty debe ser mayor a 0" });

        var storeId = GetActiveStoreId() ?? (int?)HttpContext.Items["StoreId"];
        var operatorSessionId = (int?)HttpContext.Items["SessionId"];
        var note = BuildTransformationNotes(request.SupplierId, request.SourceQty, targetQty, yieldFactor.Value, request.Notes);

        await _stockService.ApplyMovementAsync(
            request.SourceProductId,
            StockBucket.VENDIBLE.ToString(),
            -request.SourceQty,
            StockMovementType.Transformation.ToString(),
            operatorSessionId: operatorSessionId,
            notes: note,
            storeId: storeId
        );

        await _stockService.ApplyMovementAsync(
            request.TargetProductId,
            StockBucket.VENDIBLE.ToString(),
            targetQty,
            StockMovementType.Transformation.ToString(),
            operatorSessionId: operatorSessionId,
            notes: note,
            storeId: storeId
        );

        var observedFactor = request.SourceQty > 0
            ? Math.Round(targetQty / request.SourceQty, 6, MidpointRounding.AwayFromZero)
            : 0m;

        decimal? deviationPct = null;
        if (request.SuggestedYieldFactor.HasValue && request.SuggestedYieldFactor.Value > 0)
        {
            deviationPct = Math.Round(
                Math.Abs((observedFactor - request.SuggestedYieldFactor.Value) / request.SuggestedYieldFactor.Value) * 100m,
                3,
                MidpointRounding.AwayFromZero
            );
        }

        var eventEntity = new TransformationYieldEvent
        {
            StoreId = storeId,
            SupplierId = request.SupplierId,
            SourceProductId = request.SourceProductId,
            TargetProductId = request.TargetProductId,
            SourceQty = request.SourceQty,
            TargetQty = targetQty,
            YieldFactorObserved = observedFactor,
            SuggestedYieldFactor = request.SuggestedYieldFactor,
            UsedSuggestedFactor = request.UsedSuggestedFactor ?? false,
            DeviationPct = deviationPct,
            SuggestionConfidence = request.SuggestionConfidence,
            SuggestionSampleCount = request.SuggestionSampleCount,
            SuggestionSource = request.SuggestionSource,
            OperatorSessionId = operatorSessionId,
            UserId = (int?)HttpContext.Items["SessionUsuarioId"],
            AppliedAt = DateTime.UtcNow
        };

        _context.TransformationYieldEvents.Add(eventEntity);
        await _context.SaveChangesAsync();
        await RecalculateYieldProfileAsync(storeId, request.SupplierId, request.SourceProductId, request.TargetProductId);
        await EvaluateYieldRecalibrationAsync(storeId, request.SupplierId, request.SourceProductId, request.TargetProductId);

        return Ok(new
        {
            sourceProductId = request.SourceProductId,
            targetProductId = request.TargetProductId,
            sourceQty = request.SourceQty,
            targetQty,
            yieldFactor,
            observedFactor,
            storeId
        });
    }

    [HttpGet("transformations/yield-suggestion")]
    public async Task<ActionResult<StockTransformationYieldSuggestionDto>> GetTransformationYieldSuggestion(
        [FromQuery] int sourceProductId,
        [FromQuery] int targetProductId,
        [FromQuery] int? supplierId,
        [FromQuery] decimal? sourceQty)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        if (sourceProductId <= 0 || targetProductId <= 0) return BadRequest(new { message = "sourceProductId y targetProductId son requeridos" });
        if (sourceProductId == targetProductId) return BadRequest(new { message = "Producto origen y destino deben ser distintos" });

        var storeId = GetActiveStoreId() ?? (int?)HttpContext.Items["StoreId"];
        var templates = await GetTransformationTemplatesInternalAsync();

        var suggestion = await ResolveYieldSuggestionAsync(
            storeId,
            supplierId,
            sourceProductId,
            targetProductId,
            sourceQty,
            templates);

        return Ok(suggestion);
    }

    [HttpGet("transformations/yield-history")]
    public async Task<ActionResult<object>> GetTransformationYieldHistory(
        [FromQuery] int sourceProductId,
        [FromQuery] int targetProductId,
        [FromQuery] int? supplierId,
        [FromQuery] int take = 30)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        if (sourceProductId <= 0 || targetProductId <= 0) return BadRequest(new { message = "sourceProductId y targetProductId son requeridos" });

        var storeId = GetActiveStoreId() ?? (int?)HttpContext.Items["StoreId"];
        take = Math.Clamp(take, 5, 200);

        var query = _context.TransformationYieldEvents
            .AsNoTracking()
            .Where(e => e.SourceProductId == sourceProductId && e.TargetProductId == targetProductId);

        if (storeId.HasValue) query = query.Where(e => e.StoreId == storeId.Value);
        if (supplierId.HasValue) query = query.Where(e => e.SupplierId == supplierId.Value);

        var rows = await query
            .OrderByDescending(e => e.AppliedAt)
            .Take(take)
            .Select(e => new
            {
                e.AppliedAt,
                e.SourceQty,
                e.TargetQty,
                e.YieldFactorObserved,
                e.SuggestedYieldFactor,
                e.UsedSuggestedFactor,
                e.DeviationPct,
                e.SuggestionConfidence,
                e.SuggestionSampleCount,
                e.SuggestionSource
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("transformations/yield-policy")]
    public async Task<ActionResult<StockTransformationYieldPolicyDto>> GetTransformationYieldPolicy()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var policy = await GetYieldPolicyAsync();
        return Ok(new StockTransformationYieldPolicyDto
        {
            AutoUpdateEnabled = policy.AutoUpdateEnabled,
            RequireAdminApproval = policy.RequireAdminApproval,
            MinSampleCount = policy.MinSampleCount,
            MaxVolatility = policy.MaxVolatility,
            MaxDeviationPct = policy.MaxDeviationPct,
            MinDeviationPct = policy.MinDeviationPct
        });
    }

    [HttpPut("transformations/yield-policy")]
    public async Task<ActionResult<StockTransformationYieldPolicyDto>> UpdateTransformationYieldPolicy([FromBody] StockTransformationYieldPolicyDto request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var normalized = NormalizeYieldPolicy(new TransformationYieldPolicyData
        {
            AutoUpdateEnabled = request.AutoUpdateEnabled,
            RequireAdminApproval = request.RequireAdminApproval,
            MinSampleCount = request.MinSampleCount,
            MaxVolatility = request.MaxVolatility,
            MaxDeviationPct = request.MaxDeviationPct,
            MinDeviationPct = request.MinDeviationPct
        });
        await SaveYieldPolicyAsync(normalized);

        return Ok(new StockTransformationYieldPolicyDto
        {
            AutoUpdateEnabled = normalized.AutoUpdateEnabled,
            RequireAdminApproval = normalized.RequireAdminApproval,
            MinSampleCount = normalized.MinSampleCount,
            MaxVolatility = normalized.MaxVolatility,
            MaxDeviationPct = normalized.MaxDeviationPct,
            MinDeviationPct = normalized.MinDeviationPct
        });
    }

    [HttpGet("transformations/yield-recalibrations")]
    public async Task<ActionResult<List<StockTransformationYieldRecalibrationDto>>> GetTransformationYieldRecalibrations([FromQuery] string? status = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var storeId = GetActiveStoreId() ?? (int?)HttpContext.Items["StoreId"];
        var query = _context.TransformationYieldRecalibrationLogs.AsNoTracking().AsQueryable();
        if (storeId.HasValue) query = query.Where(x => x.StoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);

        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new StockTransformationYieldRecalibrationDto
            {
                Id = x.Id,
                SupplierId = x.SupplierId,
                SourceProductId = x.SourceProductId,
                TargetProductId = x.TargetProductId,
                CurrentYieldFactor = x.CurrentYieldFactor,
                ProposedYieldFactor = x.ProposedYieldFactor,
                DeviationPct = x.DeviationPct,
                SampleCount = x.SampleCount,
                Volatility = x.Volatility,
                Status = x.Status,
                DecisionNotes = x.DecisionNotes,
                ApprovedByUserId = x.ApprovedByUserId,
                CreatedAt = x.CreatedAt,
                DecidedAt = x.DecidedAt
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost("transformations/yield-recalibrations/{id}/approve")]
    public async Task<ActionResult<object>> ApproveTransformationYieldRecalibration(int id, [FromBody] DecideStockTransformationYieldRecalibrationDto? request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var row = await _context.TransformationYieldRecalibrationLogs.FirstOrDefaultAsync(x => x.Id == id);
        if (row == null) return NotFound();
        if (!string.Equals(row.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "La recalibración ya fue procesada" });

        var templates = await GetTransformationTemplatesInternalAsync();
        var template = templates.FirstOrDefault(t =>
            t.SupplierId == row.SupplierId
            && t.SourceProductId == row.SourceProductId
            && t.TargetProductId == row.TargetProductId);
        if (template == null)
        {
            template = new StockTransformationTemplateData
            {
                SupplierId = row.SupplierId,
                SourceProductId = row.SourceProductId,
                TargetProductId = row.TargetProductId,
                YieldFactor = row.ProposedYieldFactor,
                UpdatedAt = DateTime.UtcNow,
                Notes = "Auto-recalibración aprobada"
            };
            templates.Add(template);
        }
        else
        {
            template.YieldFactor = row.ProposedYieldFactor;
            template.UpdatedAt = DateTime.UtcNow;
            template.Notes = string.IsNullOrWhiteSpace(template.Notes)
                ? "Auto-recalibración aprobada"
                : $"{template.Notes}. Auto-recalibración aprobada";
        }

        await SaveTransformationTemplatesInternalAsync(templates);

        row.Status = "ApprovedApplied";
        row.DecisionNotes = request?.Notes;
        row.DecidedAt = DateTime.UtcNow;
        row.ApprovedByUserId = (int?)HttpContext.Items["SessionUsuarioId"];
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recalibración aplicada correctamente" });
    }

    [HttpPost("transformations/yield-recalibrations/{id}/reject")]
    public async Task<ActionResult<object>> RejectTransformationYieldRecalibration(int id, [FromBody] DecideStockTransformationYieldRecalibrationDto? request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var row = await _context.TransformationYieldRecalibrationLogs.FirstOrDefaultAsync(x => x.Id == id);
        if (row == null) return NotFound();
        if (!string.Equals(row.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "La recalibración ya fue procesada" });

        row.Status = "Rejected";
        row.DecisionNotes = request?.Notes;
        row.DecidedAt = DateTime.UtcNow;
        row.ApprovedByUserId = (int?)HttpContext.Items["SessionUsuarioId"];
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recalibración rechazada" });
    }

    private int? GetActiveStoreId()
    {
        var raw = Request.Headers["X-Store-Id"].FirstOrDefault();
        if (int.TryParse(raw, out var id)) return id;
        return null;
    }

    private static DateTime? ToUtcOrNull(DateTime? value)
    {
        if (!value.HasValue) return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private async Task<bool> IsAdminOrManagerAsync()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var userIdObj) || userIdObj is not int userId)
            return false;

        var user = await _context.Usuarios.FindAsync(userId);
        if (user == null)
            return false;

        return user.Role == UserRole.Admin.ToString()
            || user.Role == UserRole.Supervisor.ToString()
            || user.Role == "Manager";
    }

    private async Task<List<StockTransformationTemplateData>> GetTransformationTemplatesInternalAsync()
    {
        var raw = await _context.Settings
            .Where(s => s.Key == TransformationTemplatesSettingKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(raw)) return new List<StockTransformationTemplateData>();

        try
        {
            return JsonSerializer.Deserialize<List<StockTransformationTemplateData>>(raw) ?? new List<StockTransformationTemplateData>();
        }
        catch
        {
            return new List<StockTransformationTemplateData>();
        }
    }

    private async Task SaveTransformationTemplatesInternalAsync(List<StockTransformationTemplateData> templates)
    {
        var serialized = JsonSerializer.Serialize(templates);
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == TransformationTemplatesSettingKey);
        if (setting == null)
        {
            _context.Settings.Add(new Setting
            {
                Key = TransformationTemplatesSettingKey,
                Value = serialized,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = serialized;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<TransformationYieldPolicyData> GetYieldPolicyAsync()
    {
        var raw = await _context.Settings
            .Where(s => s.Key == TransformationYieldPolicySettingKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(raw)) return new TransformationYieldPolicyData();

        try
        {
            var parsed = JsonSerializer.Deserialize<TransformationYieldPolicyData>(raw);
            return NormalizeYieldPolicy(parsed ?? new TransformationYieldPolicyData());
        }
        catch
        {
            return new TransformationYieldPolicyData();
        }
    }

    private async Task SaveYieldPolicyAsync(TransformationYieldPolicyData policy)
    {
        var serialized = JsonSerializer.Serialize(policy);
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == TransformationYieldPolicySettingKey);
        if (setting == null)
        {
            _context.Settings.Add(new Setting
            {
                Key = TransformationYieldPolicySettingKey,
                Value = serialized,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = serialized;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private static TransformationYieldPolicyData NormalizeYieldPolicy(TransformationYieldPolicyData policy)
    {
        var minDeviation = Math.Clamp(policy.MinDeviationPct, 0m, 50m);
        var maxDeviation = Math.Clamp(policy.MaxDeviationPct, 1m, 100m);
        if (minDeviation > maxDeviation)
        {
            minDeviation = maxDeviation;
        }

        return new TransformationYieldPolicyData
        {
            AutoUpdateEnabled = policy.AutoUpdateEnabled,
            RequireAdminApproval = policy.RequireAdminApproval,
            MinSampleCount = Math.Clamp(policy.MinSampleCount, 3, 100),
            MaxVolatility = Math.Clamp(policy.MaxVolatility, 0.01m, 1m),
            MaxDeviationPct = maxDeviation,
            MinDeviationPct = minDeviation
        };
    }

    private async Task EvaluateYieldRecalibrationAsync(int? storeId, int? supplierId, int sourceProductId, int targetProductId)
    {
        var policy = await GetYieldPolicyAsync();
        if (!policy.AutoUpdateEnabled) return;

        var profile = await _context.TransformationYieldProfiles.FirstOrDefaultAsync(p =>
            p.StoreId == storeId
            && p.SupplierId == supplierId
            && p.SourceProductId == sourceProductId
            && p.TargetProductId == targetProductId);
        if (profile == null) return;
        if (profile.SampleCount < policy.MinSampleCount) return;
        if (profile.Volatility > policy.MaxVolatility) return;

        var templates = await GetTransformationTemplatesInternalAsync();
        var template = templates.FirstOrDefault(t =>
            t.SupplierId == supplierId
            && t.SourceProductId == sourceProductId
            && t.TargetProductId == targetProductId);
        if (template == null) return;
        if (template.YieldFactor <= 0) return;

        var deviationPct = Math.Round(Math.Abs((profile.SuggestedYieldFactor - template.YieldFactor) / template.YieldFactor) * 100m, 3, MidpointRounding.AwayFromZero);
        if (deviationPct < policy.MinDeviationPct || deviationPct > policy.MaxDeviationPct) return;

        if (policy.RequireAdminApproval)
        {
            var existingPending = await _context.TransformationYieldRecalibrationLogs
                .FirstOrDefaultAsync(l => l.Status == "Pending"
                    && l.StoreId == storeId
                    && l.SupplierId == supplierId
                    && l.SourceProductId == sourceProductId
                    && l.TargetProductId == targetProductId);

            if (existingPending != null)
            {
                existingPending.ProposedYieldFactor = profile.SuggestedYieldFactor;
                existingPending.CurrentYieldFactor = template.YieldFactor;
                existingPending.DeviationPct = deviationPct;
                existingPending.SampleCount = profile.SampleCount;
                existingPending.Volatility = profile.Volatility;
                existingPending.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.TransformationYieldRecalibrationLogs.Add(new TransformationYieldRecalibrationLog
                {
                    StoreId = storeId,
                    SupplierId = supplierId,
                    SourceProductId = sourceProductId,
                    TargetProductId = targetProductId,
                    CurrentYieldFactor = template.YieldFactor,
                    ProposedYieldFactor = profile.SuggestedYieldFactor,
                    DeviationPct = deviationPct,
                    SampleCount = profile.SampleCount,
                    Volatility = profile.Volatility,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return;
        }

        var previousFactor = template.YieldFactor;
        template.YieldFactor = profile.SuggestedYieldFactor;
        template.UpdatedAt = DateTime.UtcNow;
        template.Notes = string.IsNullOrWhiteSpace(template.Notes)
            ? "Auto-recalibración aplicada"
            : $"{template.Notes}. Auto-recalibración aplicada";
        await SaveTransformationTemplatesInternalAsync(templates);

        _context.TransformationYieldRecalibrationLogs.Add(new TransformationYieldRecalibrationLog
        {
            StoreId = storeId,
            SupplierId = supplierId,
            SourceProductId = sourceProductId,
            TargetProductId = targetProductId,
            CurrentYieldFactor = previousFactor,
            ProposedYieldFactor = profile.SuggestedYieldFactor,
            DeviationPct = deviationPct,
            SampleCount = profile.SampleCount,
            Volatility = profile.Volatility,
            Status = "AppliedAuto",
            CreatedAt = DateTime.UtcNow,
            DecidedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private async Task<StockTransformationYieldSuggestionDto> ResolveYieldSuggestionAsync(
        int? storeId,
        int? supplierId,
        int sourceProductId,
        int targetProductId,
        decimal? sourceQty,
        List<StockTransformationTemplateData> templates)
    {
        var now = DateTime.UtcNow;
        var profile = await FindYieldProfileAsync(storeId, supplierId, sourceProductId, targetProductId);

        if (profile != null)
        {
            return new StockTransformationYieldSuggestionDto
            {
                SupplierId = supplierId,
                SourceProductId = sourceProductId,
                TargetProductId = targetProductId,
                SuggestedYieldFactor = profile.SuggestedYieldFactor,
                ExpectedTargetQty = sourceQty.HasValue ? Math.Round(sourceQty.Value * profile.SuggestedYieldFactor, 3, MidpointRounding.AwayFromZero) : null,
                Confidence = profile.Confidence,
                SampleCount = profile.SampleCount,
                Volatility = profile.Volatility,
                Source = "Perfil",
                CalculatedAt = now
            };
        }

        var template = templates.FirstOrDefault(t =>
            t.SupplierId == supplierId
            && t.SourceProductId == sourceProductId
            && t.TargetProductId == targetProductId)
            ?? templates.FirstOrDefault(t =>
                t.SupplierId == null
                && t.SourceProductId == sourceProductId
                && t.TargetProductId == targetProductId);

        var factor = template?.YieldFactor ?? 1m;
        return new StockTransformationYieldSuggestionDto
        {
            SupplierId = supplierId,
            SourceProductId = sourceProductId,
            TargetProductId = targetProductId,
            SuggestedYieldFactor = factor,
            ExpectedTargetQty = sourceQty.HasValue ? Math.Round(sourceQty.Value * factor, 3, MidpointRounding.AwayFromZero) : null,
            Confidence = template != null ? "Baja" : "Sin datos",
            SampleCount = 0,
            Volatility = 0,
            Source = template != null ? "Plantilla" : "Default",
            CalculatedAt = now
        };
    }

    private async Task<TransformationYieldProfile?> FindYieldProfileAsync(int? storeId, int? supplierId, int sourceProductId, int targetProductId)
    {
        var baseQuery = _context.TransformationYieldProfiles
            .AsNoTracking()
            .Where(p => p.SourceProductId == sourceProductId && p.TargetProductId == targetProductId);

        if (storeId.HasValue) baseQuery = baseQuery.Where(p => p.StoreId == storeId.Value);

        if (supplierId.HasValue)
        {
            var exact = await baseQuery.FirstOrDefaultAsync(p => p.SupplierId == supplierId.Value);
            if (exact != null) return exact;
        }

        return await baseQuery.FirstOrDefaultAsync(p => p.SupplierId == null);
    }

    private async Task RecalculateYieldProfileAsync(int? storeId, int? supplierId, int sourceProductId, int targetProductId)
    {
        var query = _context.TransformationYieldEvents
            .Where(e => e.SourceProductId == sourceProductId && e.TargetProductId == targetProductId);

        if (storeId.HasValue) query = query.Where(e => e.StoreId == storeId.Value);
        if (supplierId.HasValue) query = query.Where(e => e.SupplierId == supplierId.Value);
        else query = query.Where(e => e.SupplierId == null);

        var values = await query
            .OrderByDescending(e => e.AppliedAt)
            .Select(e => e.YieldFactorObserved)
            .Take(60)
            .ToListAsync();

        if (values.Count == 0) return;

        var filtered = FilterOutliers(values);
        var median = Median(filtered);
        var volatility = RelativeStdDev(filtered, median);
        var confidence = BuildConfidenceLabel(filtered.Count, volatility);

        var profile = await _context.TransformationYieldProfiles.FirstOrDefaultAsync(p =>
            p.StoreId == storeId
            && p.SupplierId == supplierId
            && p.SourceProductId == sourceProductId
            && p.TargetProductId == targetProductId);

        if (profile == null)
        {
            profile = new TransformationYieldProfile
            {
                StoreId = storeId,
                SupplierId = supplierId,
                SourceProductId = sourceProductId,
                TargetProductId = targetProductId
            };
            _context.TransformationYieldProfiles.Add(profile);
        }

        profile.SuggestedYieldFactor = median;
        profile.SampleCount = filtered.Count;
        profile.Volatility = volatility;
        profile.Confidence = confidence;
        profile.LastRecalculatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static List<decimal> FilterOutliers(List<decimal> values)
    {
        if (values.Count < 6) return values;
        var sorted = values.OrderBy(v => v).ToList();
        var q1 = Percentile(sorted, 25m);
        var q3 = Percentile(sorted, 75m);
        var iqr = q3 - q1;
        if (iqr <= 0) return sorted;

        var min = q1 - (1.5m * iqr);
        var max = q3 + (1.5m * iqr);
        return sorted.Where(v => v >= min && v <= max).ToList();
    }

    private static decimal Median(List<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;
        if (count == 0) return 1m;
        if (count % 2 == 1) return sorted[count / 2];
        return Math.Round((sorted[(count / 2) - 1] + sorted[count / 2]) / 2m, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal Percentile(List<decimal> sorted, decimal percentile)
    {
        if (sorted.Count == 0) return 0;
        if (sorted.Count == 1) return sorted[0];
        var position = (sorted.Count - 1) * (percentile / 100m);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper) return sorted[lower];

        var weight = position - lower;
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * weight);
    }

    private static decimal RelativeStdDev(List<decimal> values, decimal center)
    {
        if (values.Count <= 1 || center <= 0) return 0;
        var mean = values.Average();
        var variance = values.Sum(v => (double)((v - mean) * (v - mean))) / values.Count;
        var stdDev = (decimal)Math.Sqrt(variance);
        return Math.Round(stdDev / center, 6, MidpointRounding.AwayFromZero);
    }

    private static string BuildConfidenceLabel(int sampleCount, decimal volatility)
    {
        if (sampleCount >= 12 && volatility <= 0.12m) return "Alta";
        if (sampleCount >= 5 && volatility <= 0.25m) return "Media";
        return "Baja";
    }

    private static StockTransformationTemplateDto ToTransformationTemplateDto(StockTransformationTemplateData data)
    {
        return new StockTransformationTemplateDto
        {
            SupplierId = data.SupplierId,
            SourceProductId = data.SourceProductId,
            TargetProductId = data.TargetProductId,
            YieldFactor = data.YieldFactor,
            Notes = data.Notes,
            UpdatedAt = data.UpdatedAt
        };
    }

    private static string BuildTransformationNotes(int? supplierId, decimal sourceQty, decimal targetQty, decimal yieldFactor, string? notes)
    {
        var prefix = $"Transformacion stock{(supplierId.HasValue ? $" proveedor:{supplierId.Value}" : string.Empty)} origen:{sourceQty:0.###} destino:{targetQty:0.###} factor:{yieldFactor:0.###}";
        return string.IsNullOrWhiteSpace(notes) ? prefix : $"{prefix}. {notes}";
    }

    private static SupplierClaimDto BuildSupplierClaimDto(SupplierClaim claim)
    {
        return new SupplierClaimDto
        {
            Id = claim.Id,
            SupplierId = claim.SupplierId,
            SupplierName = claim.Supplier?.Name,
            PurchaseId = claim.PurchaseId,
            Status = claim.Status,
            HasReceipt = claim.HasReceipt,
            ReceiptType = claim.ReceiptType,
            ReceiptNumber = claim.ReceiptNumber,
            Notes = claim.Notes,
            RequestedSettlementMode = claim.RequestedSettlementMode,
            ResolvedSettlementMode = claim.ResolvedSettlementMode,
            CreatedAt = claim.CreatedAt,
            PickedUpAt = claim.PickedUpAt,
            CreditedAt = claim.CreditedAt,
            ResolvedAt = claim.ResolvedAt,
            ResolvedByUserId = claim.ResolvedByUserId,
            Items = claim.Items.Select(i => new SupplierClaimItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductCode = i.Product?.Barcode,
                ProductName = i.Product?.Name,
                Quantity = i.Quantity,
                UnitCostSnapshot = i.UnitCostSnapshot,
                Notes = i.Notes
            }).ToList(),
            Evidences = claim.Evidences
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new SupplierClaimEvidenceDto
                {
                    Id = e.Id,
                    FileName = e.FileName,
                    ContentType = e.ContentType,
                    FileSize = e.FileSize,
                    CreatedAt = e.CreatedAt,
                    PreviewUrl = $"/api/v1/stock/claims/{claim.Id}/evidences/{e.Id}/preview",
                    DownloadUrl = $"/api/v1/stock/claims/{claim.Id}/evidences/{e.Id}/download"
                })
                .ToList(),
            ExchangeLines = claim.ExchangeLines.Select(l => new SupplierClaimExchangeLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductName = l.Product?.Name,
                Quantity = l.Quantity,
                UnitCostSnapshot = l.UnitCostSnapshot,
                Notes = l.Notes
            }).ToList(),
            Refunds = claim.Refunds.Select(r => new SupplierClaimRefundDto
            {
                Id = r.Id,
                Amount = r.Amount,
                Notes = r.Notes,
                CreatedByUserId = r.CreatedByUserId,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }

    private sealed class StockTransformationTemplateData
    {
        public int? SupplierId { get; set; }
        public int SourceProductId { get; set; }
        public int TargetProductId { get; set; }
        public decimal YieldFactor { get; set; }
        public string? Notes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class TransformationYieldPolicyData
    {
        public bool AutoUpdateEnabled { get; set; } = false;
        public bool RequireAdminApproval { get; set; } = true;
        public int MinSampleCount { get; set; } = 12;
        public decimal MaxVolatility { get; set; } = 0.12m;
        public decimal MaxDeviationPct { get; set; } = 15m;
        public decimal MinDeviationPct { get; set; } = 3m;
    }

    public sealed class CreateSupplierClaimWithEvidenceRequest
    {
        public int? SupplierId { get; set; }
        public int? PurchaseId { get; set; }
        public bool HasReceipt { get; set; }
        public string? ReceiptType { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Notes { get; set; }
        public string? SettlementMode { get; set; }
        public string ItemsJson { get; set; } = "[]";
        public List<IFormFile>? EvidenceFiles { get; set; }
    }
}
