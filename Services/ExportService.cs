using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;

namespace KodvianSuperMarket.Services;

public record ExportFileResult(byte[] Content, string ContentType, string FileName);

public interface IExportService
{
    Task<ExportFileResult> BuildAsync(string reportKey, string title, string? subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows, string format, int? storeId = null);
}

public class ExportService : IExportService
{
    private readonly ApplicationDbContext _db;

    public ExportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ExportFileResult> BuildAsync(string reportKey, string title, string? subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows, string format, int? storeId = null)
    {
        var normalized = string.IsNullOrWhiteSpace(format) ? "xlsx" : format.Trim().ToLowerInvariant();
        if (normalized is "excel") normalized = "xlsx";

        if (normalized == "pdf")
        {
            var branding = await ResolveBrandingAsync(storeId);
            var pdfBytes = BuildPdf(title, subtitle, headers, rows, branding);
            return new ExportFileResult(pdfBytes, "application/pdf", $"{SanitizeFileName(reportKey)}-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
        }

        var xlsxBytes = BuildExcel(title, headers, rows);
        return new ExportFileResult(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{SanitizeFileName(reportKey)}-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    private static byte[] BuildExcel(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(TrimSheetName(sheetName));

        for (var i = 0; i < headers.Count; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < headers.Count; c++)
            {
                var value = c < row.Count ? row[c] : null;
                ws.Cell(r + 2, c + 1).Value = ToCellValue(value);
            }
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildPdf(string title, string? subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows, PrintBrandingDto branding)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Spacing(2);
                    header.Item().Text(branding.DisplayName).Bold().FontSize(15);
                    if (!string.IsNullOrWhiteSpace(branding.TicketHeaderText))
                        header.Item().Text(branding.TicketHeaderText).SemiBold();
                    header.Item().Text(title).Bold().FontSize(11);
                    if (!string.IsNullOrWhiteSpace(subtitle))
                        header.Item().Text(subtitle!);
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < headers.Count; i++)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text(h).Bold();
                        }
                    });

                    foreach (var row in rows)
                    {
                        for (var c = 0; c < headers.Count; c++)
                        {
                            var value = c < row.Count ? row[c] : null;
                            table.Cell().Element(CellStyle).Text(ToPdfText(value));
                        }
                    }

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);
                    }
                });

                page.Footer().AlignCenter().Text($"{branding.TicketFooterText} - Generado {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8);
            });
        }).GeneratePdf();
    }

    private async Task<PrintBrandingDto> ResolveBrandingAsync(int? storeId)
    {
        int? tenantId = null;
        if (storeId.HasValue)
            tenantId = await _db.Stores.Where(s => s.Id == storeId.Value).Select(s => (int?)s.TenantId).FirstOrDefaultAsync();

        if (!tenantId.HasValue)
            tenantId = await _db.Tenants.OrderBy(t => t.Id).Select(t => (int?)t.Id).FirstOrDefaultAsync();

        if (!tenantId.HasValue)
            return new PrintBrandingDto();

        var branding = await _db.TenantBrandingSettings.FirstOrDefaultAsync(b => b.TenantId == tenantId.Value);
        if (branding == null)
            return new PrintBrandingDto();

        return new PrintBrandingDto
        {
            DisplayName = string.IsNullOrWhiteSpace(branding.DisplayName) ? "Kodvian SuperMarket" : branding.DisplayName,
            LogoUrl = branding.LogoUrl,
            TicketHeaderText = string.IsNullOrWhiteSpace(branding.TicketHeaderText) ? "Exportacion gerencial" : branding.TicketHeaderText,
            TicketFooterText = string.IsNullOrWhiteSpace(branding.TicketFooterText) ? "Documento interno" : branding.TicketFooterText,
            ReturnPolicyText = string.IsNullOrWhiteSpace(branding.ReturnPolicyText) ? "" : branding.ReturnPolicyText
        };
    }

    private static XLCellValue ToCellValue(object? value)
    {
        if (value == null) return string.Empty;

        return value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.UtcDateTime,
            decimal dec => dec,
            double dbl => dbl,
            float fl => fl,
            int i => i,
            long l => l,
            bool b => b,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string ToPdfText(object? value)
    {
        if (value == null) return "-";
        return value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm"),
            DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm"),
            decimal dec => dec.ToString("0.###"),
            double dbl => dbl.ToString("0.###"),
            float fl => fl.ToString("0.###"),
            _ => value.ToString() ?? "-"
        };
    }

    private static string TrimSheetName(string value)
    {
        var safe = string.IsNullOrWhiteSpace(value) ? "Export" : value.Trim();
        var invalid = new[] { '[', ']', '*', '?', '/', '\\', ':' };
        foreach (var ch in invalid)
            safe = safe.Replace(ch, '-');

        return safe.Length <= 31 ? safe : safe[..31];
    }

    private static string SanitizeFileName(string value)
    {
        var safe = string.IsNullOrWhiteSpace(value) ? "export" : value.Trim().ToLowerInvariant();
        foreach (var ch in Path.GetInvalidFileNameChars())
            safe = safe.Replace(ch, '-');
        return safe.Replace(' ', '-');
    }
}
