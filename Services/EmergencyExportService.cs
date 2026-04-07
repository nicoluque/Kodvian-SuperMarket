using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IEmergencyExportService
{
    Task<byte[]> GenerateEmergencyCatalogPdfAsync(int top, bool includeNoBarcode, bool includeCigarettes, int? tenantId = null);
}

public class EmergencyExportService : IEmergencyExportService
{
    private readonly ApplicationDbContext _context;

    public EmergencyExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateEmergencyCatalogPdfAsync(int top, bool includeNoBarcode, bool includeCigarettes, int? tenantId = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var topLimit = top <= 0 ? 100 : top;
        var fromDate = DateTime.UtcNow.AddDays(-30);

        var topProducts = await _context.SaleItems
            .Include(si => si.Product)
            .Where(si => si.ProductId != null && si.Sale.CreatedAt >= fromDate)
            .GroupBy(si => si.ProductId!.Value)
            .Select(g => new
            {
                ProductId = g.Key,
                Qty = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Qty)
            .Take(topLimit)
            .ToListAsync();

        var topProductIds = topProducts.Select(x => x.ProductId).ToList();
        var products = await _context.Products
            .Where(p => topProductIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync();

        var defaultPriceListId = await _context.PriceLists
            .Where(pl => pl.IsDefault)
            .Select(pl => pl.Id)
            .FirstOrDefaultAsync();

        var prices = await _context.ProductPrices
            .Where(pp => pp.PriceListId == defaultPriceListId)
            .GroupBy(pp => pp.ProductId)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToDictionaryAsync(pp => pp.ProductId, pp => pp);

        var noBarcode = includeNoBarcode
            ? await _context.Products.Where(p => string.IsNullOrWhiteSpace(p.Barcode) && !string.IsNullOrWhiteSpace(p.QuickCode)).OrderBy(p => p.SaleType).ThenBy(p => p.Name).ToListAsync()
            : new List<Product>();

        var cigarettes = includeCigarettes
            ? await _context.Products.Where(p => p.IsCigarette).OrderBy(p => p.Name).ToListAsync()
            : new List<Product>();

        TenantBrandingSettings? branding = null;
        if (tenantId.HasValue)
            branding = await _context.TenantBrandingSettings.FirstOrDefaultAsync(b => b.TenantId == tenantId.Value);

        var brandName = branding?.DisplayName ?? "Kodvian SuperMarket";
        var ticketHeader = branding?.TicketHeaderText ?? "Catalogo de emergencia";
        var ticketFooter = branding?.TicketFooterText ?? "Conserve este material para contingencias";
        var supportLine = string.Join(" | ", new[]
        {
            string.IsNullOrWhiteSpace(branding?.SupportPhone) ? null : $"Soporte: {branding!.SupportPhone}",
            string.IsNullOrWhiteSpace(branding?.SupportEmail) ? null : $"Email: {branding!.SupportEmail}"
        }.Where(x => x != null));

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(h =>
                {
                    h.Item().Text(brandName).Bold().FontSize(16);
                    h.Item().Text($"{ticketHeader} - {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").Bold().FontSize(11);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text($"1) Top {topLimit} por ventas (ultimos 30 dias)").Bold();
                    foreach (var p in products)
                    {
                        var price = prices.TryGetValue(p.Id, out var pp)
                            ? (p.SaleType == SaleType.Weight.ToString() ? pp.PricePerKg : pp.Price)
                            : (p.SaleType == SaleType.Weight.ToString() ? p.DefaultPricePerKg : p.DefaultPrice);

                        var codeValue = !string.IsNullOrWhiteSpace(p.QuickCode) ? $"QC:{p.QuickCode}" : $"PID:{p.Id}";
                        var qrBytes = BuildQr(codeValue);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"{Truncate(p.Name, 48)}").SemiBold();
                                c.Item().Text($"Code: {codeValue} | QuickCode: {p.QuickCode ?? "-"} | SaleType: {p.SaleType} | Precio General: {price:0.00}");
                            });
                            row.ConstantItem(58).Height(58).Image(qrBytes);
                        });
                    }

                    if (includeNoBarcode)
                    {
                        col.Item().Text("2) Productos sin barcode (QuickCode) agrupados por rubro").Bold();
                        var grouped = noBarcode.GroupBy(p => p.SaleType);
                        foreach (var g in grouped)
                        {
                            col.Item().Text($"Rubro: {g.Key}").SemiBold();
                            foreach (var p in g)
                            {
                                var price = prices.TryGetValue(p.Id, out var pp)
                                    ? (p.SaleType == SaleType.Weight.ToString() ? pp.PricePerKg : pp.Price)
                                    : (p.SaleType == SaleType.Weight.ToString() ? p.DefaultPricePerKg : p.DefaultPrice);
                                var codeValue = !string.IsNullOrWhiteSpace(p.QuickCode) ? $"QC:{p.QuickCode}" : $"PID:{p.Id}";
                                col.Item().Text($"- {Truncate(p.Name, 44)} | {codeValue} | {price:0.00} | {p.SaleType}");
                            }
                        }
                    }

                    if (includeCigarettes)
                    {
                        col.Item().Text("3) Cigarrillos").Bold();
                        foreach (var p in cigarettes)
                        {
                            var price = prices.TryGetValue(p.Id, out var pp)
                                ? pp.Price
                                : p.DefaultPrice;
                            var codeValue = !string.IsNullOrWhiteSpace(p.QuickCode) ? $"QC:{p.QuickCode}" : $"PID:{p.Id}";
                            col.Item().Text($"- {Truncate(p.Name, 44)} | {codeValue} | {price:0.00} | {p.SaleType}");
                        }
                    }
                });

                page.Footer().Column(f =>
                {
                    f.Item().Text(ticketFooter).FontSize(9);
                    if (!string.IsNullOrWhiteSpace(supportLine))
                        f.Item().Text(supportLine!).FontSize(9);
                });
            });
        }).GeneratePdf();

        return pdf;
    }

    private static byte[] BuildQr(string value)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(qrCodeData);
        return png.GetGraphic(6);
    }

    private static string Truncate(string value, int max)
    {
        if (value.Length <= max)
            return value;
        return value[..max] + "...";
    }
}
