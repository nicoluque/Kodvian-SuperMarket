using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/admin/branding")]
public class BrandingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public BrandingController(ApplicationDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Get()
    {
        var tenantId = await ResolveTenantIdAsync();
        var branding = await EnsureBrandingAsync(tenantId);
        return Ok(ToDto(branding));
    }

    [HttpPut]
    public async Task<ActionResult<object>> Put([FromBody] TenantBrandingUpsertRequest request)
    {
        var tenantId = await ResolveTenantIdAsync();
        var branding = await EnsureBrandingAsync(tenantId);

        branding.DisplayName = request.DisplayName?.Trim() ?? branding.DisplayName;
        branding.LogoUrl = request.LogoUrl?.Trim() ?? branding.LogoUrl;
        branding.PrimaryColor = NormalizeHex(request.PrimaryColor, branding.PrimaryColor);
        branding.SecondaryColor = NormalizeHex(request.SecondaryColor, branding.SecondaryColor);
        branding.TicketHeaderText = request.TicketHeaderText?.Trim() ?? branding.TicketHeaderText;
        branding.TicketFooterText = request.TicketFooterText?.Trim() ?? branding.TicketFooterText;
        branding.ReturnPolicyText = request.ReturnPolicyText?.Trim() ?? branding.ReturnPolicyText;
        branding.SupportPhone = request.SupportPhone?.Trim();
        branding.SupportEmail = request.SupportEmail?.Trim();
        branding.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(branding));
    }

    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<object>> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Logo file is required" });

        var tenantId = await ResolveTenantIdAsync();
        var branding = await EnsureBrandingAsync(tenantId);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".svg" && ext != ".webp")
            return BadRequest(new { message = "Unsupported logo format" });

        var storagePath = _config["App:StoragePath"] ?? "storage";
        var fullStoragePath = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.GetFullPath(Path.Combine(_env.ContentRootPath, storagePath));

        var logosDir = Path.Combine(fullStoragePath, "branding");
        Directory.CreateDirectory(logosDir);

        var fileName = $"tenant-{tenantId}-logo{ext}";
        var fullPath = Path.Combine(logosDir, fileName);
        await using (var fs = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(fs);
        }

        branding.LogoUrl = $"/api/v1/admin/branding/logo/{fileName}";
        branding.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { logoUrl = branding.LogoUrl });
    }

    [HttpGet("logo/{fileName}")]
    public IActionResult GetLogo(string fileName)
    {
        var storagePath = _config["App:StoragePath"] ?? "storage";
        var fullStoragePath = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.GetFullPath(Path.Combine(_env.ContentRootPath, storagePath));

        var logosDir = Path.Combine(fullStoragePath, "branding");
        var fullPath = Path.Combine(logosDir, fileName);
        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return PhysicalFile(fullPath, contentType);
    }

    [HttpGet("preview/ticket")]
    public async Task<ActionResult<object>> PreviewTicket()
    {
        var tenantId = await ResolveTenantIdAsync();
        var branding = await EnsureBrandingAsync(tenantId);

        return Ok(new
        {
            html = $"<div style='font-family:Arial,sans-serif;padding:12px;border:1px dashed #ccc;max-width:340px'>"
                 + $"<h3 style='margin:0;color:{branding.PrimaryColor}'>{Escape(branding.DisplayName)}</h3>"
                 + $"<p style='margin:6px 0'>{Escape(branding.TicketHeaderText)}</p>"
                 + "<p>Ticket #000123 - Total: $ 12.450,00</p>"
                 + $"<p style='font-size:12px;color:#555'>{Escape(branding.ReturnPolicyText)}</p>"
                 + $"<p style='font-size:12px'>{Escape(branding.TicketFooterText)}</p>"
                 + "</div>"
        });
    }

    [HttpGet("preview/login")]
    public async Task<ActionResult<object>> PreviewLogin()
    {
        var tenantId = await ResolveTenantIdAsync();
        var branding = await EnsureBrandingAsync(tenantId);

        return Ok(new
        {
            html = $"<div style='font-family:Arial,sans-serif;padding:16px;border-radius:10px;background:linear-gradient(120deg,{branding.PrimaryColor}, {branding.SecondaryColor});color:#fff'>"
                 + $"<strong style='font-size:20px'>{Escape(branding.DisplayName)}</strong>"
                 + "<div style='margin-top:8px'>Acceso Backoffice</div>"
                 + "</div>"
        });
    }

    private async Task<int> ResolveTenantIdAsync()
    {
        if (HttpContext.Items.TryGetValue("TenantId", out var tenantObj) && tenantObj is int tenantId && tenantId > 0)
            return tenantId;

        if (HttpContext.Items.TryGetValue("SessionUsuarioId", out var userObj) && userObj is int userId)
        {
            var userTenant = await _db.Usuarios.Where(u => u.Id == userId).Select(u => u.TenantId).FirstOrDefaultAsync();
            if (userTenant.HasValue)
                return userTenant.Value;
        }

        var storeHeader = Request.Headers["X-Store-Id"].FirstOrDefault();
        if (int.TryParse(storeHeader, out var storeId) && storeId > 0)
        {
            var storeTenant = await _db.Stores.Where(s => s.Id == storeId).Select(s => s.TenantId).FirstOrDefaultAsync();
            if (storeTenant > 0)
                return storeTenant;
        }

        var fallbackTenant = await _db.Tenants.OrderBy(t => t.Id).FirstOrDefaultAsync();
        if (fallbackTenant != null)
            return fallbackTenant.Id;

        var created = new Tenant { Name = "Default Tenant", Code = "default" };
        _db.Tenants.Add(created);
        await _db.SaveChangesAsync();
        return created.Id;
    }

    private async Task<TenantBrandingSettings> EnsureBrandingAsync(int tenantId)
    {
        var branding = await _db.TenantBrandingSettings.FirstOrDefaultAsync(b => b.TenantId == tenantId);
        if (branding != null)
            return branding;

        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant == null)
            throw new InvalidOperationException("Tenant not found");

        branding = new TenantBrandingSettings
        {
            TenantId = tenantId,
            DisplayName = tenant.Name,
            PrimaryColor = "#1f7f57",
            SecondaryColor = "#27313f",
            TicketHeaderText = "Gracias por su compra",
            TicketFooterText = "Conserve su comprobante",
            ReturnPolicyText = "Cambios dentro de 24h con ticket",
            UpdatedAt = DateTime.UtcNow
        };

        _db.TenantBrandingSettings.Add(branding);
        await _db.SaveChangesAsync();
        return branding;
    }

    private static object ToDto(TenantBrandingSettings b) => new
    {
        b.TenantId,
        b.DisplayName,
        b.LogoUrl,
        b.PrimaryColor,
        b.SecondaryColor,
        b.TicketHeaderText,
        b.TicketFooterText,
        b.ReturnPolicyText,
        b.SupportPhone,
        b.SupportEmail,
        b.UpdatedAt
    };

    private static string NormalizeHex(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var v = value.Trim();
        if (!v.StartsWith("#"))
            v = "#" + v;

        if (v.Length != 7)
            return fallback;

        for (var i = 1; i < v.Length; i++)
        {
            var c = char.ToLowerInvariant(v[i]);
            var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
            if (!isHex)
                return fallback;
        }

        return v;
    }

    private static string Escape(string? raw)
    {
        return System.Net.WebUtility.HtmlEncode(raw ?? string.Empty);
    }
}

public class TenantBrandingUpsertRequest
{
    public string? DisplayName { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? TicketHeaderText { get; set; }
    public string? TicketFooterText { get; set; }
    public string? ReturnPolicyText { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
}
