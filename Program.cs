using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Middleware;
using KodvianSuperMarket.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var storagePath = builder.Configuration["App:StoragePath"] ?? "storage";
var logsPath = Path.Combine(storagePath, "logs");
Directory.CreateDirectory(logsPath);

builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(logsPath, "api-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, shared: true));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddSingleton<IRequestDeduplicationService, RequestDeduplicationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IOperatorSessionService, OperatorSessionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<ICashSessionService, CashSessionService>();
builder.Services.AddScoped<IShiftCloseGate, RealShiftCloseGate>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ICigaretteCountService, CigaretteCountService>();
builder.Services.AddScoped<IRRHHService, RRHHService>();
builder.Services.AddScoped<IKanbanService, KanbanService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IProductLookupService, ProductLookupService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IEmergencyExportService, EmergencyExportService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IPurchaseSuggestionService, PurchaseSuggestionService>();
builder.Services.AddScoped<IStoreShiftService, StoreShiftService>();
builder.Services.AddHostedService<LateFeeBackgroundService>();

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var traceId = System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        var details = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido" : e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new
        {
            code = "VALIDATION_ERROR",
            message = "Error de validación",
            details,
            traceId
        });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<RequestContextEnrichmentMiddleware>();

app.MapControllers();

app.Run();
