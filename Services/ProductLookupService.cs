using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IProductLookupService
{
    Task<Product?> ResolveByScannedCodeAsync(string scannedCode);
}

public class ProductLookupService : IProductLookupService
{
    private readonly ApplicationDbContext _context;

    public ProductLookupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> ResolveByScannedCodeAsync(string scannedCode)
    {
        var code = scannedCode.Trim();

        if (code.StartsWith("QC:", StringComparison.OrdinalIgnoreCase))
        {
            var quickCode = code.Substring(3);
            return await _context.Products.FirstOrDefaultAsync(p => p.QuickCode == quickCode);
        }

        if (code.StartsWith("PID:", StringComparison.OrdinalIgnoreCase))
        {
            var raw = code.Substring(4);
            if (int.TryParse(raw, out var productId))
            {
                return await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            }
            return null;
        }

        var byBarcode = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == code);
        if (byBarcode != null)
            return byBarcode;

        return await _context.Products.FirstOrDefaultAsync(p => p.QuickCode == code);
    }
}
