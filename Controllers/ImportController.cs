using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/import")]
[DeviceAuth]
[OperatorSessionAuth]
public class ImportController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stockService;
    private readonly IRequestDeduplicationService _requestDeduplication;

    public ImportController(ApplicationDbContext db, IStockService stockService, IRequestDeduplicationService requestDeduplication)
    {
        _db = db;
        _stockService = stockService;
        _requestDeduplication = requestDeduplication;
    }

    [HttpGet("templates/products")]
    public async Task<IActionResult> ProductsTemplate()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Template("products-template.xlsx", "nombre", "codigoBarras", "codigoRapido", "tipoVenta", "unidad", "precio", "precioPorKg", "permitePrecioManual", "esCigarrillo");
    }

    [HttpGet("templates/customers")]
    public async Task<IActionResult> CustomersTemplate()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Template("customers-template.xlsx", "nombreCompleto", "dni", "direccion", "telefono", "permiteCredito", "limiteCredito");
    }

    [HttpGet("templates/suppliers")]
    public async Task<IActionResult> SuppliersTemplate()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Template("suppliers-template.xlsx", "nombre", "cuit", "direccion", "telefono", "email");
    }

    [HttpGet("templates/prices")]
    public async Task<IActionResult> PricesTemplate()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Template("prices-template.xlsx", "codigoBarras", "codigoRapido", "precio", "precioPorKg");
    }

    [HttpGet("templates/stock-opening")]
    public async Task<IActionResult> StockOpeningTemplate()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Template("stock-opening-template.xlsx", "codigoBarras", "codigoRapido", "nombreProducto", "cantidadVendible", "cantidadReclamo", "cantidadMerma");
    }

    [HttpGet("templates/catalog-stock")]
    public async Task<IActionResult> CatalogStockTemplate()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Template("catalog-stock-template.xlsx",
            "nombre", "codigoBarras", "codigoRapido", "tipoVenta", "unidad", "precio", "precioPorKg", "permitePrecioManual", "esCigarrillo",
            "cantidadVendible", "cantidadReclamo", "cantidadMerma");
    }

    [HttpGet("templates/stock-adjustments")]
    public async Task<IActionResult> StockAdjustmentsTemplate()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Template("stock-adjustments-template.xlsx", "codigoBarras", "codigoRapido", "nombreProducto", "cantAjusteVendible", "cantAjusteReclamo", "cantAjusteMerma");
    }

    [HttpPost("stock-opening/preview")]
    public async Task<ActionResult<StockOpeningPreviewResponse>> PreviewStockOpening([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var rows = ReadRows(req.File);
        var session = new StockCountSession
        {
            SessionType = StockCountSessionType.OpeningBalance.ToString(),
            Status = StockCountSessionStatus.Previewed.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var hasPriorActivity = await _db.Sales.AnyAsync() || await _db.Purchases.AnyAsync() || await _db.StockMovements.AnyAsync();
        if (hasPriorActivity)
        {
            session.WarningMessage = "ATENCION: ya existen ventas/compras/movimientos previos. Requiere confirmacion explicita para commit.";
            session.ExplicitConfirmation = false;
        }

        _db.StockCountSessions.Add(session);
        await _db.SaveChangesAsync();

        foreach (var row in rows)
        {
            var rowNumber = int.Parse(row["__row"]);
            var barcode = Get(row, "barcode");
            var quickCode = Get(row, "quickCode");
            var requestedName = Get(row, "productName");

            var product = await _db.Products.FirstOrDefaultAsync(p =>
                (!string.IsNullOrWhiteSpace(barcode) && p.Barcode == barcode) ||
                (!string.IsNullOrWhiteSpace(quickCode) && p.QuickCode == quickCode));

            var currentVendible = 0m;
            var currentReclamo = 0m;
            var currentMerma = 0m;
            string? error = null;

            if (product != null)
            {
                var stocks = await _db.ProductStocks.Where(ps => ps.ProductId == product.Id).ToListAsync();
                currentVendible = stocks.FirstOrDefault(s => s.Bucket == StockBucket.VENDIBLE.ToString())?.Quantity ?? 0m;
                currentReclamo = stocks.FirstOrDefault(s => s.Bucket == StockBucket.RECLAMO.ToString())?.Quantity ?? 0m;
                currentMerma = stocks.FirstOrDefault(s => s.Bucket == StockBucket.MERMA.ToString())?.Quantity ?? 0m;
            }
            else
            {
                error = "Producto no encontrado por barcode/quickCode";
            }

            var targetVendible = ParseDecimal(Get(row, "vendibleQty"));
            var targetReclamo = ParseDecimal(Get(row, "reclamoQty"));
            var targetMerma = ParseDecimal(Get(row, "mermaQty"));

            _db.StockCountLines.Add(new StockCountLine
            {
                StockCountSessionId = session.Id,
                ProductId = product?.Id,
                Barcode = EmptyToNull(barcode),
                QuickCode = EmptyToNull(quickCode),
                ProductName = product?.Name ?? EmptyToNull(requestedName),
                RowNumber = rowNumber,
                CurrentVendibleQty = currentVendible,
                CurrentReclamoQty = currentReclamo,
                CurrentMermaQty = currentMerma,
                TargetVendibleQty = targetVendible,
                TargetReclamoQty = targetReclamo,
                TargetMermaQty = targetMerma,
                DeltaVendibleQty = targetVendible - currentVendible,
                DeltaReclamoQty = targetReclamo - currentReclamo,
                DeltaMermaQty = targetMerma - currentMerma,
                Error = error
            });
        }

        await _db.SaveChangesAsync();

        var lines = await _db.StockCountLines.Where(l => l.StockCountSessionId == session.Id).OrderBy(l => l.RowNumber).ToListAsync();
        return Ok(ToPreviewResponse(session, lines));
    }

    [HttpPost("catalog-stock/preview")]
    public async Task<ActionResult<ImportPreviewResponse>> PreviewCatalogStock([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Ok(await BuildCatalogStockPreview(req.File, req.Upsert));
    }

    [HttpPost("catalog-stock/commit")]
    public async Task<ActionResult<object>> CommitCatalogStock([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var preview = await BuildCatalogStockPreview(req.File, req.Upsert);
        if (preview.InvalidRows > 0)
        {
            return BadRequest(new
            {
                message = "Se detectaron filas con error. Corrige el archivo y vuelve a intentar. No se aplicaron cambios.",
                preview
            });
        }

        var created = 0;
        var updated = 0;
        var stockMovementsApplied = 0;
        var movementPlan = new List<(Product product, decimal vendibleQty, decimal reclamoQty, decimal mermaQty)>();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var row in preview.Rows.Select(r => r.Data))
            {
                var (product, wasCreated) = await UpsertProductEntity(row);
                if (wasCreated) created++; else updated++;

                movementPlan.Add((
                    product,
                    ParseDecimal(Get(row, "vendibleQty")),
                    ParseDecimal(Get(row, "reclamoQty")),
                    ParseDecimal(Get(row, "mermaQty"))
                ));
            }

            await _db.SaveChangesAsync();

            foreach (var movement in movementPlan)
            {
                if (movement.vendibleQty > 0)
                {
                    await _stockService.ApplyMovementAsync(movement.product.Id, StockBucket.VENDIBLE.ToString(), movement.vendibleQty,
                        StockMovementType.Initial.ToString(), notes: "Importacion unificada de catalogo + stock");
                    stockMovementsApplied++;
                }

                if (movement.reclamoQty > 0)
                {
                    await _stockService.ApplyMovementAsync(movement.product.Id, StockBucket.RECLAMO.ToString(), movement.reclamoQty,
                        StockMovementType.Initial.ToString(), notes: "Importacion unificada de catalogo + stock");
                    stockMovementsApplied++;
                }

                if (movement.mermaQty > 0)
                {
                    await _stockService.ApplyMovementAsync(movement.product.Id, StockBucket.MERMA.ToString(), movement.mermaQty,
                        StockMovementType.Initial.ToString(), notes: "Importacion unificada de catalogo + stock");
                    stockMovementsApplied++;
                }
            }

            await SaveCommit("catalog-stock", req.Upsert, preview, created, updated);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return Ok(new
        {
            preview.JobId,
            preview.TotalRows,
            preview.ValidRows,
            preview.InvalidRows,
            created,
            updated,
            stockMovementsApplied
        });
    }

    [HttpPost("stock-adjustments/preview")]
    public async Task<ActionResult<ImportPreviewResponse>> PreviewStockAdjustments([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return Ok(await BuildStockAdjustmentsPreview(req.File));
    }

    [HttpPost("stock-adjustments/commit")]
    public async Task<ActionResult<object>> CommitStockAdjustments([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var preview = await BuildStockAdjustmentsPreview(req.File);
        if (preview.InvalidRows > 0)
        {
            return BadRequest(new
            {
                message = "Se detectaron filas con error. Corrige el archivo y vuelve a intentar. No se aplicaron cambios.",
                preview
            });
        }

        var adjustmentsApplied = 0;
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var row in preview.Rows.Select(r => r.Data))
            {
                var product = await FindProductByCodes(Get(row, "barcode"), Get(row, "quickCode"));
                if (product == null)
                    throw new InvalidOperationException("Producto no encontrado durante commit de ajustes");

                var vendibleDelta = ParseDecimal(Get(row, "vendibleDelta"));
                var reclamoDelta = ParseDecimal(Get(row, "reclamoDelta"));
                var mermaDelta = ParseDecimal(Get(row, "mermaDelta"));

                if (vendibleDelta != 0)
                {
                    await _stockService.ApplyMovementAsync(product.Id, StockBucket.VENDIBLE.ToString(), vendibleDelta,
                        StockMovementType.Adjustment.ToString(), notes: "Ajuste masivo de stock");
                    adjustmentsApplied++;
                }

                if (reclamoDelta != 0)
                {
                    await _stockService.ApplyMovementAsync(product.Id, StockBucket.RECLAMO.ToString(), reclamoDelta,
                        StockMovementType.Adjustment.ToString(), notes: "Ajuste masivo de stock");
                    adjustmentsApplied++;
                }

                if (mermaDelta != 0)
                {
                    await _stockService.ApplyMovementAsync(product.Id, StockBucket.MERMA.ToString(), mermaDelta,
                        StockMovementType.Adjustment.ToString(), notes: "Ajuste masivo de stock");
                    adjustmentsApplied++;
                }
            }

            await SaveCommit("stock-adjustments", true, preview, 0, 0);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return Ok(new
        {
            preview.JobId,
            preview.TotalRows,
            preview.ValidRows,
            preview.InvalidRows,
            adjustmentsApplied
        });
    }

    [HttpPost("stock-opening/commit")]
    public async Task<ActionResult<object>> CommitStockOpening([FromBody] StockOpeningCommitRequest request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var session = await _db.StockCountSessions.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == request.SessionId);
        if (session == null) return NotFound(new { message = "Session not found" });
        if (session.Status == StockCountSessionStatus.Committed.ToString()) return BadRequest(new { message = "Session already committed" });
        if (session.Status == StockCountSessionStatus.Cancelled.ToString()) return BadRequest(new { message = "Session cancelled" });

        using var dedup = _requestDeduplication.Acquire($"stock-opening/commit/{request.SessionId}");

        var hasPriorActivity = await _db.Sales.AnyAsync() || await _db.Purchases.AnyAsync() || await _db.StockMovements.AnyAsync();
        if (hasPriorActivity && !request.ExplicitConfirmation)
        {
            return BadRequest(new
            {
                message = "ATENCION: hay ventas/compras/movimientos previos. Reenviar con explicitConfirmation=true para continuar.",
                requiresExplicitConfirmation = true
            });
        }

        var operatorSessionId = HttpContext.Items.TryGetValue("SessionId", out var sidObj) && sidObj is int sid ? sid : (int?)null;
        var deviceId = HttpContext.Items.TryGetValue("DeviceId", out var didObj) && didObj is int did ? did : (int?)null;

        foreach (var line in session.Lines.Where(l => string.IsNullOrWhiteSpace(l.Error) && l.ProductId.HasValue))
        {
            if (line.DeltaVendibleQty != 0)
            {
                await _stockService.ApplyMovementAsync(line.ProductId!.Value, StockBucket.VENDIBLE.ToString(), line.DeltaVendibleQty,
                    "OPENING_BALANCE_VENDIBLE", operatorSessionId: operatorSessionId, deviceId: deviceId, notes: $"Stock opening session {session.Id}");
            }

            if (line.DeltaReclamoQty != 0)
            {
                await _stockService.ApplyMovementAsync(line.ProductId!.Value, StockBucket.RECLAMO.ToString(), line.DeltaReclamoQty,
                    "OPENING_BALANCE_RECLAMO", operatorSessionId: operatorSessionId, deviceId: deviceId, notes: $"Stock opening session {session.Id}");
            }

            if (line.DeltaMermaQty != 0)
            {
                await _stockService.ApplyMovementAsync(line.ProductId!.Value, StockBucket.MERMA.ToString(), line.DeltaMermaQty,
                    "OPENING_BALANCE_MERMA", operatorSessionId: operatorSessionId, deviceId: deviceId, notes: $"Stock opening session {session.Id}");
            }
        }

        session.Status = StockCountSessionStatus.Committed.ToString();
        session.CommittedAt = DateTime.UtcNow;
        session.ExplicitConfirmation = request.ExplicitConfirmation;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            sessionId = session.Id,
            status = session.Status,
            committedAt = session.CommittedAt,
            totalLines = session.Lines.Count,
            errorLines = session.Lines.Count(l => !string.IsNullOrWhiteSpace(l.Error))
        });
    }

    [HttpPost("products/preview")]
    public async Task<ActionResult<ImportPreviewResponse>> PreviewProducts([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Preview("products", req.File, req.Upsert);
    }

    [HttpPost("products/commit")]
    public async Task<ActionResult<object>> CommitProducts([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Commit("products", req.File, req.Upsert);
    }

    [HttpPost("customers/preview")]
    public async Task<ActionResult<ImportPreviewResponse>> PreviewCustomers([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Preview("customers", req.File, req.Upsert);
    }

    [HttpPost("customers/commit")]
    public async Task<ActionResult<object>> CommitCustomers([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Commit("customers", req.File, req.Upsert);
    }

    [HttpPost("suppliers/preview")]
    public async Task<ActionResult<ImportPreviewResponse>> PreviewSuppliers([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Preview("suppliers", req.File, req.Upsert);
    }

    [HttpPost("suppliers/commit")]
    public async Task<ActionResult<object>> CommitSuppliers([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Commit("suppliers", req.File, req.Upsert);
    }

    [HttpPost("prices/preview")]
    public async Task<ActionResult<ImportPreviewResponse>> PreviewPrices([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Preview("prices", req.File, req.Upsert);
    }

    [HttpPost("prices/commit")]
    public async Task<ActionResult<object>> CommitPrices([FromForm] ImportUploadRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        return await Commit("prices", req.File, req.Upsert);
    }

    private IActionResult Template(string fileName, params string[] headers)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("template");
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task<ActionResult<ImportPreviewResponse>> Preview(string type, IFormFile file, bool upsert)
    {
        var rows = ReadRows(file);
        var resultRows = new List<ImportRowResult>();

        foreach (var row in rows)
        {
            var rowNum = int.Parse(row["__row"]);
            var errors = await ValidateRow(type, row, upsert);
            resultRows.Add(new ImportRowResult(rowNum, errors.Count == 0, errors, row));
        }

        var preview = await SavePreview(type, upsert, resultRows);
        return Ok(preview);
    }

    private async Task<ActionResult<object>> Commit(string type, IFormFile file, bool upsert)
    {
        var previewResult = await Preview(type, file, upsert);
        if (previewResult.Result is not OkObjectResult ok || ok.Value is not ImportPreviewResponse preview)
            return BadRequest(new { message = "No se pudo generar la vista previa" });

        var created = 0;
        var updated = 0;

        foreach (var row in preview.Rows.Where(r => r.Valid).Select(r => r.Data))
        {
            switch (type)
            {
                case "products":
                    (created, updated) = await UpsertProduct(row, created, updated);
                    break;
                case "customers":
                    (created, updated) = await UpsertCustomer(row, created, updated);
                    break;
                case "suppliers":
                    (created, updated) = await UpsertSupplier(row, created, updated);
                    break;
                case "prices":
                    (created, updated) = await UpsertPrice(row, created, updated);
                    break;
            }
        }

        await _db.SaveChangesAsync();
        await SaveCommit(type, upsert, preview, created, updated);

        return Ok(new { preview.JobId, preview.TotalRows, preview.ValidRows, preview.InvalidRows, created, updated, errors = preview.Rows.Where(r => !r.Valid) });
    }

    private async Task<ImportPreviewResponse> BuildCatalogStockPreview(IFormFile file, bool upsert)
    {
        var rows = ReadRows(file);
        var resultRows = new List<ImportRowResult>();

        foreach (var row in rows)
        {
            var rowNum = int.Parse(row["__row"]);
            var errors = await ValidateRow("products", row, upsert);

            var vendibleQty = ParseDecimal(Get(row, "vendibleQty"));
            var reclamoQty = ParseDecimal(Get(row, "reclamoQty"));
            var mermaQty = ParseDecimal(Get(row, "mermaQty"));

            if (vendibleQty < 0) errors.Add(new("vendibleQty", "cantidad vendible no puede ser negativa"));
            if (reclamoQty < 0) errors.Add(new("reclamoQty", "cantidad reclamo no puede ser negativa"));
            if (mermaQty < 0) errors.Add(new("mermaQty", "cantidad merma no puede ser negativa"));

            var product = await FindProductByCodes(Get(row, "barcode"), Get(row, "quickCode"));
            row["catalogAction"] = product == null ? "crear" : "actualizar";
            row["totalQtyImpact"] = (vendibleQty + reclamoQty + mermaQty).ToString("0.###");

            resultRows.Add(new ImportRowResult(rowNum, errors.Count == 0, errors, row));
        }

        return await SavePreview("catalog-stock", upsert, resultRows);
    }

    private async Task<ImportPreviewResponse> BuildStockAdjustmentsPreview(IFormFile file)
    {
        var rows = ReadRows(file);
        var resultRows = new List<ImportRowResult>();

        foreach (var row in rows)
        {
            var rowNum = int.Parse(row["__row"]);
            var errors = new List<ImportRowError>();

            var barcode = Get(row, "barcode");
            var quickCode = Get(row, "quickCode");
            if (string.IsNullOrWhiteSpace(barcode) && string.IsNullOrWhiteSpace(quickCode))
                errors.Add(new("barcode/quickCode", "codigo de barras o codigo rapido requerido"));

            var product = await FindProductByCodes(barcode, quickCode);
            if (product == null)
            {
                errors.Add(new("barcode/quickCode", "producto no encontrado"));
                row["productNameResolved"] = "-";
            }
            else
            {
                row["productNameResolved"] = product.Name;

                var vendibleDelta = ParseDecimal(Get(row, "vendibleDelta"));
                var reclamoDelta = ParseDecimal(Get(row, "reclamoDelta"));
                var mermaDelta = ParseDecimal(Get(row, "mermaDelta"));

                var currentVendible = await GetStockQty(product.Id, StockBucket.VENDIBLE.ToString());
                var currentReclamo = await GetStockQty(product.Id, StockBucket.RECLAMO.ToString());
                var currentMerma = await GetStockQty(product.Id, StockBucket.MERMA.ToString());

                var resultVendible = currentVendible + vendibleDelta;
                var resultReclamo = currentReclamo + reclamoDelta;
                var resultMerma = currentMerma + mermaDelta;

                if (resultVendible < 0) errors.Add(new("vendibleDelta", $"no alcanza stock vendible para descontar. actual: {currentVendible:0.###}"));
                if (resultReclamo < 0) errors.Add(new("reclamoDelta", $"no alcanza stock reclamo para descontar. actual: {currentReclamo:0.###}"));
                if (resultMerma < 0) errors.Add(new("mermaDelta", $"no alcanza stock merma para descontar. actual: {currentMerma:0.###}"));

                row["currentVendibleQty"] = currentVendible.ToString("0.###");
                row["currentReclamoQty"] = currentReclamo.ToString("0.###");
                row["currentMermaQty"] = currentMerma.ToString("0.###");
                row["resultVendibleQty"] = resultVendible.ToString("0.###");
                row["resultReclamoQty"] = resultReclamo.ToString("0.###");
                row["resultMermaQty"] = resultMerma.ToString("0.###");
            }

            resultRows.Add(new ImportRowResult(rowNum, errors.Count == 0, errors, row));
        }

        return await SavePreview("stock-adjustments", true, resultRows);
    }

    private async Task<List<ImportRowError>> ValidateRow(string type, Dictionary<string, string> row, bool upsert)
    {
        var errors = new List<ImportRowError>();
        if (type == "products")
        {
            var saleType = NormalizeSaleType(Get(row, "saleType"));
            if (saleType == null) errors.Add(new("saleType", "tipo de venta invalido (usa Unidad, Peso o Paquete)"));
            if (string.IsNullOrWhiteSpace(Get(row, "name"))) errors.Add(new("name", "nombre requerido"));
            if (saleType == "Weight" && ParseDecimal(Get(row, "pricePerKg")) <= 0) errors.Add(new("pricePerKg", "precio por kg requerido"));
            if (saleType != "Weight" && ParseDecimal(Get(row, "price")) <= 0) errors.Add(new("price", "precio requerido"));

            var barcode = Get(row, "barcode");
            var quickCode = Get(row, "quickCode");
            var exists = await _db.Products.AnyAsync(p => (!string.IsNullOrWhiteSpace(barcode) && p.Barcode == barcode) || (!string.IsNullOrWhiteSpace(quickCode) && p.QuickCode == quickCode));
            if (!upsert && exists) errors.Add(new("barcode/quickCode", "producto existente"));
            if (string.IsNullOrWhiteSpace(barcode) && string.IsNullOrWhiteSpace(quickCode)) errors.Add(new("barcode/quickCode", "codigo de barras o codigo rapido requerido"));
        }
        else if (type == "customers")
        {
            if (string.IsNullOrWhiteSpace(Get(row, "fullName"))) errors.Add(new("fullName", "nombre completo requerido"));
        }
        else if (type == "suppliers")
        {
            if (string.IsNullOrWhiteSpace(Get(row, "name"))) errors.Add(new("name", "nombre requerido"));
        }
        else if (type == "prices")
        {
            var barcode = Get(row, "barcode");
            var quickCode = Get(row, "quickCode");
            var exists = await _db.Products.AnyAsync(p => (!string.IsNullOrWhiteSpace(barcode) && p.Barcode == barcode) || (!string.IsNullOrWhiteSpace(quickCode) && p.QuickCode == quickCode));
            if (!exists) errors.Add(new("barcode/quickCode", "producto no encontrado"));
        }
        return errors;
    }

    private async Task<(int created, int updated)> UpsertProduct(Dictionary<string, string> row, int created, int updated)
    {
        var (_, wasCreated) = await UpsertProductEntity(row);
        if (wasCreated) created++; else updated++;
        return (created, updated);
    }

    private async Task<(Product product, bool wasCreated)> UpsertProductEntity(Dictionary<string, string> row)
    {
        var barcode = Get(row, "barcode");
        var quickCode = Get(row, "quickCode");
        var p = await FindProductByCodes(barcode, quickCode);
        var wasCreated = p == null;
        if (p == null)
        {
            p = new Product { CreatedAt = DateTime.UtcNow };
            _db.Products.Add(p);
        }

        p.Name = Get(row, "name");
        p.Barcode = EmptyToNull(barcode);
        p.QuickCode = EmptyToNull(quickCode);
        p.SaleType = NormalizeSaleType(Get(row, "saleType")) ?? "Unit";
        p.UnitName = EmptyToNull(Get(row, "unitName"));
        p.DefaultPrice = ParseDecimal(Get(row, "price"));
        p.DefaultPricePerKg = ParseDecimal(Get(row, "pricePerKg"));
        p.AllowsManualPrice = ParseBool(Get(row, "allowsManualPrice"));
        p.IsCigarette = ParseBool(Get(row, "isCigarette"));
        p.UpdatedAt = DateTime.UtcNow;
        return (p, wasCreated);
    }

    private Task<Product?> FindProductByCodes(string barcode, string quickCode)
    {
        return _db.Products.FirstOrDefaultAsync(x =>
            (!string.IsNullOrWhiteSpace(barcode) && x.Barcode == barcode) ||
            (!string.IsNullOrWhiteSpace(quickCode) && x.QuickCode == quickCode));
    }

    private async Task<decimal> GetStockQty(int productId, string bucket)
    {
        return await _db.ProductStocks
            .Where(s => s.ProductId == productId && s.Bucket == bucket)
            .Select(s => s.Quantity)
            .FirstOrDefaultAsync();
    }

    private async Task<(int created, int updated)> UpsertCustomer(Dictionary<string, string> row, int created, int updated)
    {
        var dni = Get(row, "dni");
        var c = !string.IsNullOrWhiteSpace(dni) ? await _db.Customers.FirstOrDefaultAsync(x => x.DNI == dni) : null;
        if (c == null) { c = new Customer { CreatedAt = DateTime.UtcNow }; _db.Customers.Add(c); created++; } else updated++;
        c.FullName = Get(row, "fullName"); c.DNI = EmptyToNull(dni); c.Address = EmptyToNull(Get(row, "address")); c.Phone = EmptyToNull(Get(row, "phone")); c.AllowsCredit = ParseBool(Get(row, "allowsCredit")); c.CreditLimit = ParseDecimal(Get(row, "creditLimit")); c.UpdatedAt = DateTime.UtcNow;
        return (created, updated);
    }

    private async Task<(int created, int updated)> UpsertSupplier(Dictionary<string, string> row, int created, int updated)
    {
        var cuit = Get(row, "cuit");
        var name = Get(row, "name");
        var s = !string.IsNullOrWhiteSpace(cuit) ? await _db.Suppliers.FirstOrDefaultAsync(x => x.CUIT == cuit) : await _db.Suppliers.FirstOrDefaultAsync(x => x.Name == name);
        if (s == null) { s = new Supplier { CreatedAt = DateTime.UtcNow }; _db.Suppliers.Add(s); created++; } else updated++;
        s.Name = name; s.CUIT = EmptyToNull(cuit); s.Address = EmptyToNull(Get(row, "address")); s.Phone = EmptyToNull(Get(row, "phone")); s.Email = EmptyToNull(Get(row, "email"));
        return (created, updated);
    }

    private async Task<(int created, int updated)> UpsertPrice(Dictionary<string, string> row, int created, int updated)
    {
        var barcode = Get(row, "barcode");
        var quickCode = Get(row, "quickCode");
        var product = await _db.Products.FirstAsync(x => (!string.IsNullOrWhiteSpace(barcode) && x.Barcode == barcode) || (!string.IsNullOrWhiteSpace(quickCode) && x.QuickCode == quickCode));
        var priceList = await _db.PriceLists.FirstOrDefaultAsync(p => p.IsDefault) ?? new PriceList { Name = "General", IsDefault = true, IsActive = true, CreatedAt = DateTime.UtcNow };
        if (priceList.Id == 0) { _db.PriceLists.Add(priceList); await _db.SaveChangesAsync(); }
        var pp = await _db.ProductPrices.FirstOrDefaultAsync(x => x.ProductId == product.Id && x.PriceListId == priceList.Id);
        if (pp == null) { pp = new ProductPrice { ProductId = product.Id, PriceListId = priceList.Id, CreatedAt = DateTime.UtcNow }; _db.ProductPrices.Add(pp); created++; } else updated++;
        pp.Price = ParseDecimal(Get(row, "price")); pp.PricePerKg = ParseDecimal(Get(row, "pricePerKg"));
        return (created, updated);
    }

    private async Task<ImportPreviewResponse> SavePreview(string type, bool upsert, List<ImportRowResult> rows)
    {
        var job = new ImportJob { ImportType = type, Upsert = upsert, Mode = "Preview", TotalRows = rows.Count, ValidRows = rows.Count(r => r.Valid), InvalidRows = rows.Count(r => !r.Valid), Status = "Completed", CreatedAt = DateTime.UtcNow };
        _db.ImportJobs.Add(job); await _db.SaveChangesAsync();
        var errors = rows.SelectMany(r => r.Errors.Select(e => new ImportJobError { ImportJobId = job.Id, RowNumber = r.RowNumber, Field = e.Field, Message = e.Message })).ToList();
        if (errors.Count > 0) { _db.ImportJobErrors.AddRange(errors); await _db.SaveChangesAsync(); }
        return new ImportPreviewResponse(job.Id, rows.Count, rows.Count(r => r.Valid), rows.Count(r => !r.Valid), rows);
    }

    private async Task SaveCommit(string type, bool upsert, ImportPreviewResponse preview, int created, int updated)
    {
        var job = new ImportJob { ImportType = type, Upsert = upsert, Mode = "Commit", TotalRows = preview.TotalRows, ValidRows = preview.ValidRows, InvalidRows = preview.InvalidRows, CreatedCount = created, UpdatedCount = updated, Status = "Completed", CreatedAt = DateTime.UtcNow };
        _db.ImportJobs.Add(job); await _db.SaveChangesAsync();
        var errors = preview.Rows.Where(r => !r.Valid).SelectMany(r => r.Errors.Select(e => new ImportJobError { ImportJobId = job.Id, RowNumber = r.RowNumber, Field = e.Field, Message = e.Message })).ToList();
        if (errors.Count > 0) { _db.ImportJobErrors.AddRange(errors); await _db.SaveChangesAsync(); }
    }

    private StockOpeningPreviewResponse ToPreviewResponse(StockCountSession session, List<StockCountLine> lines)
    {
        return new StockOpeningPreviewResponse
        {
            SessionId = session.Id,
            SessionType = session.SessionType,
            Status = session.Status,
            TotalRows = lines.Count,
            ErrorRows = lines.Count(l => !string.IsNullOrWhiteSpace(l.Error)),
            RequiresExplicitConfirmation = !string.IsNullOrWhiteSpace(session.WarningMessage),
            WarningMessage = session.WarningMessage,
            Lines = lines.Select(l => new StockOpeningPreviewLineDto
            {
                Id = l.Id,
                RowNumber = l.RowNumber,
                ProductId = l.ProductId,
                Barcode = l.Barcode,
                QuickCode = l.QuickCode,
                ProductName = l.ProductName,
                CurrentVendibleQty = l.CurrentVendibleQty,
                CurrentReclamoQty = l.CurrentReclamoQty,
                CurrentMermaQty = l.CurrentMermaQty,
                TargetVendibleQty = l.TargetVendibleQty,
                TargetReclamoQty = l.TargetReclamoQty,
                TargetMermaQty = l.TargetMermaQty,
                DeltaVendibleQty = l.DeltaVendibleQty,
                DeltaReclamoQty = l.DeltaReclamoQty,
                DeltaMermaQty = l.DeltaMermaQty,
                Error = l.Error
            }).ToList()
        };
    }

    private async Task<bool> IsAdminOrManagerAsync()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var userIdObj) || userIdObj is not int userId)
            return false;

        var user = await _db.Usuarios.FindAsync(userId);
        if (user == null)
            return false;

        return user.Role == UserRole.Admin.ToString() || user.Role == UserRole.Supervisor.ToString() || user.Role == "Manager";
    }

    private static List<Dictionary<string, string>> ReadRows(IFormFile file)
    {
        try
        {
            using var ms = new MemoryStream();
            file.CopyTo(ms);
            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.First();
            var headers = ws.Row(1).CellsUsed().Select(c => NormalizeHeader(c.GetString())).ToList();

            if (headers.Count == 0)
                throw new InvalidOperationException("El archivo no tiene encabezados. Usa la plantilla oficial (.xlsx).");

            var rows = new List<Dictionary<string, string>>();
            var last = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (var r = 2; r <= last; r++)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var any = false;
                for (var c = 1; c <= headers.Count; c++)
                {
                    var v = ws.Cell(r, c).GetString().Trim();
                    dict[headers[c - 1]] = v;
                    any |= !string.IsNullOrWhiteSpace(v);
                }

                if (any)
                {
                    dict["__row"] = r.ToString();
                    rows.Add(dict);
                }
            }

            return rows;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            throw new InvalidOperationException("Formato de archivo invalido. Este modulo solo acepta archivos .xlsx descargados desde la plantilla.");
        }
    }

    private static string NormalizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header)) return string.Empty;

        var normalized = header.Trim().ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ñ", "n")
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");

        return normalized switch
        {
            "name" or "nombre" => "name",
            "barcode" or "codigobarras" => "barcode",
            "quickcode" or "codigorapido" => "quickCode",
            "saletype" or "tipoventa" => "saleType",
            "unitname" or "unidad" => "unitName",
            "price" or "precio" => "price",
            "priceperkg" or "precioporkg" => "pricePerKg",
            "allowsmanualprice" or "permitepreciomanual" => "allowsManualPrice",
            "iscigarette" or "escigarrillo" => "isCigarette",
            "fullname" or "nombrecompleto" => "fullName",
            "dni" => "dni",
            "address" or "direccion" => "address",
            "phone" or "telefono" => "phone",
            "allowscredit" or "permitecredito" => "allowsCredit",
            "creditlimit" or "limitecredito" => "creditLimit",
            "cuit" => "cuit",
            "email" or "correo" => "email",
            "productname" or "nombreproducto" => "productName",
            "vendibleqty" or "cantidadvendible" => "vendibleQty",
            "reclamoqty" or "cantidadreclamo" => "reclamoQty",
            "mermaqty" or "cantidadmerma" => "mermaQty",
            "vendibledelta" or "deltavendible" => "vendibleDelta",
            "reclamodelta" or "deltareclamo" => "reclamoDelta",
            "mermadelta" or "deltamerma" => "mermaDelta",
            "cantajustevendible" => "vendibleDelta",
            "cantajustereclamo" => "reclamoDelta",
            "cantajustemerma" => "mermaDelta",
            _ => header.Trim()
        };
    }

    private static string? NormalizeSaleType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim().ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ñ", "n");

        return normalized switch
        {
            "unit" or "unidad" => "Unit",
            "weight" or "peso" => "Weight",
            "package" or "paquete" => "Package",
            _ => null
        };
    }

    private static string Get(Dictionary<string, string> row, string key) => row.TryGetValue(key, out var v) ? v.Trim() : string.Empty;
    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static decimal ParseDecimal(string value) => decimal.TryParse(value, out var n) ? n : 0m;
    private static bool ParseBool(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        value == "1" ||
        value.Equals("si", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("sí", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("verdadero", StringComparison.OrdinalIgnoreCase);
}

public class ImportUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public bool Upsert { get; set; } = true;
}

public record ImportPreviewResponse(int JobId, int TotalRows, int ValidRows, int InvalidRows, List<ImportRowResult> Rows);
public record ImportRowResult(int RowNumber, bool Valid, List<ImportRowError> Errors, Dictionary<string, string> Data);
public record ImportRowError(string Field, string Message);
