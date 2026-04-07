using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<TenantBrandingSettings> TenantBrandingSettings { get; set; } = null!;
    public DbSet<Store> Stores { get; set; } = null!;
    public DbSet<StoreUser> StoreUsers { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<OperatorSession> OperatorSessions { get; set; } = null!;
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;
    public DbSet<PaymentProviderEvent> PaymentProviderEvents { get; set; } = null!;
    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;
    public DbSet<SalePayment> SalePayments { get; set; } = null!;
    public DbSet<SaleReturn> SaleReturns { get; set; } = null!;
    public DbSet<SaleReturnLine> SaleReturnLines { get; set; } = null!;
    public DbSet<CashSessionMoneyMovement> CashSessionMoneyMovements { get; set; } = null!;
    public DbSet<CashSession> CashSessions { get; set; } = null!;
    public DbSet<CashSessionHandover> CashSessionHandovers { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerAccountMovement> CustomerAccountMovements { get; set; } = null!;
    public DbSet<CustomerMonthlyStatement> CustomerMonthlyStatements { get; set; } = null!;
    public DbSet<CustomerStatementAllocation> CustomerStatementAllocations { get; set; } = null!;
    public DbSet<ContainerMovement> ContainerMovements { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ContainerType> ContainerTypes { get; set; } = null!;
    public DbSet<PriceList> PriceLists { get; set; } = null!;
    public DbSet<ProductPrice> ProductPrices { get; set; } = null!;
    public DbSet<Promotion> Promotions { get; set; } = null!;
    public DbSet<PromotionProduct> PromotionProducts { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<PurchaseItem> PurchaseItems { get; set; } = null!;
    public DbSet<SupplierReturn> SupplierReturns { get; set; } = null!;
    public DbSet<SupplierReturnLine> SupplierReturnLines { get; set; } = null!;
    public DbSet<ExternalExchange> ExternalExchanges { get; set; } = null!;
    public DbSet<ExternalExchangeLine> ExternalExchangeLines { get; set; } = null!;
    public DbSet<PurchaseSuggestion> PurchaseSuggestions { get; set; } = null!;
    public DbSet<PurchaseSuggestionLine> PurchaseSuggestionLines { get; set; } = null!;
    public DbSet<ProductStock> ProductStocks { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;
    public DbSet<SupplierClaim> SupplierClaims { get; set; } = null!;
    public DbSet<SupplierClaimItem> SupplierClaimItems { get; set; } = null!;
    public DbSet<SupplierClaimEvidence> SupplierClaimEvidences { get; set; } = null!;
    public DbSet<SupplierClaimExchangeLine> SupplierClaimExchangeLines { get; set; } = null!;
    public DbSet<SupplierClaimRefund> SupplierClaimRefunds { get; set; } = null!;
    public DbSet<SupplierCredit> SupplierCredits { get; set; } = null!;
    public DbSet<SupplierCreditApplication> SupplierCreditApplications { get; set; } = null!;
    public DbSet<TransformationYieldEvent> TransformationYieldEvents { get; set; } = null!;
    public DbSet<TransformationYieldProfile> TransformationYieldProfiles { get; set; } = null!;
    public DbSet<TransformationYieldRecalibrationLog> TransformationYieldRecalibrationLogs { get; set; } = null!;
    public DbSet<StockCountSession> StockCountSessions { get; set; } = null!;
    public DbSet<StockCountLine> StockCountLines { get; set; } = null!;
    public DbSet<CigaretteCount> CigaretteCounts { get; set; } = null!;
    public DbSet<CigaretteCountLine> CigaretteCountLines { get; set; } = null!;
    public DbSet<TimePunch> TimePunches { get; set; } = null!;
    public DbSet<TimePunchAdjustment> TimePunchAdjustments { get; set; } = null!;
    public DbSet<EmployeeExtra> EmployeeExtras { get; set; } = null!;
    public DbSet<PayrollReceipt> PayrollReceipts { get; set; } = null!;
    public DbSet<KanbanBoard> KanbanBoards { get; set; } = null!;
    public DbSet<KanbanTask> KanbanTasks { get; set; } = null!;
    public DbSet<KanbanChecklistItem> KanbanChecklistItems { get; set; } = null!;
    public DbSet<KanbanComment> KanbanComments { get; set; } = null!;
    public DbSet<KanbanTemplate> KanbanTemplates { get; set; } = null!;
    public DbSet<TemplateChecklistItem> TemplateChecklistItems { get; set; } = null!;
    public DbSet<RecurrenceRule> RecurrenceRules { get; set; } = null!;
    public DbSet<GeneratedTask> GeneratedTasks { get; set; } = null!;
    public DbSet<ImportJob> ImportJobs { get; set; } = null!;
    public DbSet<ImportJobError> ImportJobErrors { get; set; } = null!;
    public DbSet<OnboardingSession> OnboardingSessions { get; set; } = null!;
    public DbSet<OnboardingStepState> OnboardingStepStates { get; set; } = null!;
    public DbSet<TrainingChecklist> TrainingChecklists { get; set; } = null!;
    public DbSet<TrainingChecklistItem> TrainingChecklistItems { get; set; } = null!;
    public DbSet<TrainingChecklistRun> TrainingChecklistRuns { get; set; } = null!;
    public DbSet<TrainingChecklistRunItem> TrainingChecklistRunItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.Role).HasDefaultValue("Operator");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.BrandingSettings)
                .WithOne(b => b.Tenant)
                .HasForeignKey<TenantBrandingSettings>(b => b.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantBrandingSettings>(entity =>
        {
            entity.HasIndex(e => e.TenantId).IsUnique();
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Name });

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Stores)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoreUser>(entity =>
        {
            entity.HasIndex(e => new { e.StoreId, e.UsuarioId }).IsUnique();
            entity.HasIndex(e => e.UsuarioId);

            entity.HasOne(e => e.Store)
                .WithMany(s => s.StoreUsers)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.StoreUsers)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => new { e.UsuarioId, e.IsRevoked });
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(d => d.Usuario)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ParentCashRegisterDevice)
                .WithMany(d => d.ChildDevices)
                .HasForeignKey(d => d.ParentCashRegisterDeviceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OperatorSession>(entity =>
        {
            entity.HasIndex(e => e.SessionTokenHash);
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UsuarioId, e.IsRevoked });
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(o => o.Usuario)
                .WithMany(u => u.OperatorSessions)
                .HasForeignKey(o => o.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.CreatedAt, e.EventType });
            entity.HasIndex(e => e.Success);

            entity.HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentProviderEvent>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.EventId }).IsUnique();
            entity.HasIndex(e => e.ReceivedAt);
            entity.HasIndex(e => e.ExternalReference);
            entity.Property(e => e.PayloadJson).HasDefaultValue("{}");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.DeviceId, e.Status });
            entity.HasIndex(e => e.TargetCashRegisterDeviceId);

            entity.HasOne(c => c.Device)
                .WithMany()
                .HasForeignKey(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.OperatorSession)
                .WithMany()
                .HasForeignKey(c => c.OperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(c => c.TargetCashRegisterDevice)
                .WithMany()
                .HasForeignKey(c => c.TargetCashRegisterDeviceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(e => e.CartId);
            entity.HasIndex(e => e.ProductCode);

            entity.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasIndex(e => e.CartId).IsUnique();
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.InvoiceNumber);
            entity.HasIndex(e => e.ExternalTicketId).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.DeviceId, e.Status });
            entity.HasIndex(e => e.CashSessionId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => new { e.StoreId, e.ShiftAssignmentStatus, e.CreatedAt });
            entity.HasIndex(e => new { e.StoreId, e.ShiftBucket, e.CreatedAt });

            entity.HasOne(s => s.Cart)
                .WithMany()
                .HasForeignKey(s => s.CartId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.Device)
                .WithMany()
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.OperatorSession)
                .WithMany()
                .HasForeignKey(s => s.OperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.CashSession)
                .WithMany(s => s.Sales)
                .HasForeignKey(s => s.CashSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasIndex(e => e.SaleId);
            entity.HasIndex(e => e.ProductCode);
            entity.HasIndex(e => e.ContainerTypeId);

            entity.HasOne(si => si.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(si => si.ContainerType)
                .WithMany()
                .HasForeignKey(si => si.ContainerTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalePayment>(entity =>
        {
            entity.HasIndex(e => e.SaleId);
            entity.HasIndex(e => e.PaymentMethod);
            entity.HasIndex(e => new { e.PaymentMethod, e.Status });
            entity.HasIndex(e => e.ExternalReference);

            entity.HasOne(sp => sp.Sale)
                .WithMany(s => s.Payments)
                .HasForeignKey(sp => sp.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleReturn>(entity =>
        {
            entity.HasIndex(e => e.OriginalSaleId);
            entity.HasIndex(e => e.CashSessionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CustomerId);

            entity.HasOne(e => e.OriginalSale)
                .WithMany(s => s.Returns)
                .HasForeignKey(e => e.OriginalSaleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CashSession)
                .WithMany(cs => cs.SaleReturns)
                .HasForeignKey(e => e.CashSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByUsuario)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByOperatorSession)
                .WithMany()
                .HasForeignKey(e => e.CreatedByOperatorSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleReturnLine>(entity =>
        {
            entity.HasIndex(e => e.SaleReturnId);
            entity.HasIndex(e => e.OriginalSaleItemId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(e => e.SaleReturn)
                .WithMany(r => r.Lines)
                .HasForeignKey(e => e.SaleReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.OriginalSaleItem)
                .WithMany(i => i.ReturnLines)
                .HasForeignKey(e => e.OriginalSaleItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CashSessionMoneyMovement>(entity =>
        {
            entity.HasIndex(e => e.CashSessionId);
            entity.HasIndex(e => e.Method);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.CashSession)
                .WithMany(cs => cs.MoneyMovements)
                .HasForeignKey(e => e.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUsuario)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByOperatorSession)
                .WithMany()
                .HasForeignKey(e => e.CreatedByOperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CashSession>(entity =>
        {
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OpenedAt);
            entity.HasIndex(e => new { e.DeviceId, e.Status });
            entity.HasIndex(e => e.CurrentUsuarioId);
            entity.HasIndex(e => e.OpenedByUsuarioId);
            entity.HasIndex(e => e.ClosedByUsuarioId);

            entity.HasOne(cs => cs.Device)
                .WithMany(d => d.CashSessions)
                .HasForeignKey(cs => cs.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cs => cs.OperatorSession)
                .WithMany()
                .HasForeignKey(cs => cs.OperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cs => cs.OpenedByOperatorSession)
                .WithMany()
                .HasForeignKey(cs => cs.OpenedByOperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cs => cs.OpenedByUsuario)
                .WithMany()
                .HasForeignKey(cs => cs.OpenedByUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cs => cs.CurrentOperatorSession)
                .WithMany()
                .HasForeignKey(cs => cs.CurrentOperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cs => cs.CurrentUsuario)
                .WithMany()
                .HasForeignKey(cs => cs.CurrentUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cs => cs.ClosedByOperatorSession)
                .WithMany()
                .HasForeignKey(cs => cs.ClosedByOperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cs => cs.ClosedByUsuario)
                .WithMany()
                .HasForeignKey(cs => cs.ClosedByUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CashSessionHandover>(entity =>
        {
            entity.HasIndex(e => e.CashSessionId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(h => h.CashSession)
                .WithMany(cs => cs.Handovers)
                .HasForeignKey(h => h.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.FromOperatorSession)
                .WithMany()
                .HasForeignKey(h => h.FromOperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(h => h.FromUsuario)
                .WithMany()
                .HasForeignKey(h => h.FromUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(h => h.ToOperatorSession)
                .WithMany()
                .HasForeignKey(h => h.ToOperatorSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.ToUsuario)
                .WithMany()
                .HasForeignKey(h => h.ToUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.DNI);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsAnonymous);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.IsFixedCustomer).HasDefaultValue(false);
            entity.Property(e => e.AllowsCredit).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAnonymous).HasDefaultValue(false);
        });

        modelBuilder.Entity<ContainerMovement>(entity =>
        {
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ContainerTypeId);
            entity.HasIndex(e => e.Direction);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.ContainerMovements)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ContainerType)
                .WithMany()
                .HasForeignKey(e => e.ContainerTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByOperatorSession)
                .WithMany()
                .HasForeignKey(e => e.CreatedByOperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByUsuario)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CustomerAccountMovement>(entity =>
        {
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.MovementType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.CustomerId, e.MovementType });
            entity.HasIndex(e => e.AllocatedStatementId);

            entity.HasOne(cam => cam.Customer)
                .WithMany(c => c.AccountMovements)
                .HasForeignKey(cam => cam.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerMonthlyStatement>(entity =>
        {
            entity.HasIndex(e => new { e.CustomerId, e.Year, e.Month }).IsUnique();
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.IsPaid);

            entity.HasOne(cms => cms.Customer)
                .WithMany(c => c.MonthlyStatements)
                .HasForeignKey(cms => cms.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerStatementAllocation>(entity =>
        {
            entity.HasIndex(e => e.StatementId);
            entity.HasIndex(e => e.MovementId);

            entity.HasOne(csa => csa.Statement)
                .WithMany(cms => cms.Allocations)
                .HasForeignKey(csa => csa.StatementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(csa => csa.Movement)
                .WithMany()
                .HasForeignKey(csa => csa.MovementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Barcode);
            entity.HasIndex(e => e.QuickCode);
            entity.HasIndex(e => e.CatalogStatus);
            entity.HasIndex(e => e.ContainerTypeId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.CatalogStatus).HasDefaultValue("Pending");
            entity.Property(e => e.IsCigarette).HasDefaultValue(false);
            entity.Property(e => e.AllowsManualPrice).HasDefaultValue(false);
            entity.Property(e => e.TracksExpiry).HasDefaultValue(false);
            entity.Property(e => e.StockControl).HasDefaultValue(false);

            entity.HasOne(e => e.ContainerType)
                .WithMany()
                .HasForeignKey(e => e.ContainerTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PreferredSupplier)
                .WithMany()
                .HasForeignKey(e => e.PreferredSupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ContainerType>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsDefault);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProductPrice>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.PriceListId }).IsUnique();

            entity.HasOne(pp => pp.Product)
                .WithMany(p => p.Prices)
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pp => pp.PriceList)
                .WithMany(pl => pl.ProductPrices)
                .HasForeignKey(pp => pp.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<PromotionProduct>(entity =>
        {
            entity.HasIndex(e => new { e.PromotionId, e.ProductId }).IsUnique();

            entity.HasOne(pp => pp.Promotion)
                .WithMany(p => p.Products)
                .HasForeignKey(pp => pp.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pp => pp.Product)
                .WithMany(p => p.PromotionProducts)
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => e.CUIT);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PurchaseDate);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(p => p.Supplier)
                .WithMany()
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Device)
                .WithMany()
                .HasForeignKey(p => p.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasIndex(e => e.PurchaseId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(pi => pi.Purchase)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pi => pi.Product)
                .WithMany()
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierReturn>(entity =>
        {
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.ReturnDate);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierReturnLine>(entity =>
        {
            entity.HasIndex(e => e.SupplierReturnId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(e => e.SupplierReturn)
                .WithMany(r => r.Lines)
                .HasForeignKey(e => e.SupplierReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalExchange>(entity =>
        {
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.ExchangeDate);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalExchangeLine>(entity =>
        {
            entity.HasIndex(e => e.ExternalExchangeId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Direction);

            entity.HasOne(e => e.ExternalExchange)
                .WithMany(x => x.Lines)
                .HasForeignKey(e => e.ExternalExchangeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseSuggestion>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.GeneratedAt });
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.GeneratedByUsuario)
                .WithMany()
                .HasForeignKey(e => e.GeneratedByUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PurchaseSuggestionLine>(entity =>
        {
            entity.HasIndex(e => e.PurchaseSuggestionId);
            entity.HasIndex(e => new { e.ProductId, e.Status });
            entity.HasIndex(e => e.SuggestedSupplierId);

            entity.HasOne(e => e.PurchaseSuggestion)
                .WithMany(s => s.Lines)
                .HasForeignKey(e => e.PurchaseSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SuggestedSupplier)
                .WithMany()
                .HasForeignKey(e => e.SuggestedSupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedPurchase)
                .WithMany()
                .HasForeignKey(e => e.CreatedPurchaseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProductStock>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.Bucket, e.StoreId })
                .HasFilter("\"StoreId\" IS NOT NULL")
                .IsUnique();

            entity.HasIndex(e => new { e.ProductId, e.Bucket })
                .HasFilter("\"StoreId\" IS NULL")
                .IsUnique();

            entity.HasIndex(e => e.UpdatedAt);

            entity.HasOne(ps => ps.Product)
                .WithMany()
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.MovementType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.PurchaseId);
            entity.HasIndex(e => e.SaleId);

            entity.HasOne(sm => sm.Product)
                .WithMany()
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sm => sm.Purchase)
                .WithMany()
                .HasForeignKey(sm => sm.PurchaseId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sm => sm.Sale)
                .WithMany()
                .HasForeignKey(sm => sm.SaleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StockCountSession>(entity =>
        {
            entity.HasIndex(e => e.SessionType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<StockCountLine>(entity =>
        {
            entity.HasIndex(e => e.StockCountSessionId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Barcode);
            entity.HasIndex(e => e.QuickCode);

            entity.HasOne(e => e.StockCountSession)
                .WithMany(s => s.Lines)
                .HasForeignKey(e => e.StockCountSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SupplierClaim>(entity =>
        {
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.PurchaseId);

            entity.HasOne(sc => sc.Supplier)
                .WithMany()
                .HasForeignKey(sc => sc.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sc => sc.Purchase)
                .WithMany()
                .HasForeignKey(sc => sc.PurchaseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SupplierClaimItem>(entity =>
        {
            entity.HasIndex(e => e.SupplierClaimId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(sci => sci.SupplierClaim)
                .WithMany(sc => sc.Items)
                .HasForeignKey(sci => sci.SupplierClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sci => sci.Product)
                .WithMany()
                .HasForeignKey(sci => sci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierClaimEvidence>(entity =>
        {
            entity.HasIndex(e => e.SupplierClaimId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.SupplierClaim)
                .WithMany(c => c.Evidences)
                .HasForeignKey(e => e.SupplierClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupplierClaimExchangeLine>(entity =>
        {
            entity.HasIndex(e => e.SupplierClaimId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(e => e.SupplierClaim)
                .WithMany(c => c.ExchangeLines)
                .HasForeignKey(e => e.SupplierClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierClaimRefund>(entity =>
        {
            entity.HasIndex(e => e.SupplierClaimId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.SupplierClaim)
                .WithMany(c => c.Refunds)
                .HasForeignKey(e => e.SupplierClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupplierCredit>(entity =>
        {
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.SupplierClaimId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(sc => sc.Supplier)
                .WithMany()
                .HasForeignKey(sc => sc.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sc => sc.SupplierClaim)
                .WithMany()
                .HasForeignKey(sc => sc.SupplierClaimId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SupplierCreditApplication>(entity =>
        {
            entity.HasIndex(e => e.PurchaseId);
            entity.HasIndex(e => e.SupplierCreditId);

            entity.HasOne(sca => sca.Purchase)
                .WithMany()
                .HasForeignKey(sca => sca.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sca => sca.SupplierCredit)
                .WithMany()
                .HasForeignKey(sca => sca.SupplierCreditId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransformationYieldEvent>(entity =>
        {
            entity.HasIndex(e => new { e.StoreId, e.SupplierId, e.SourceProductId, e.TargetProductId, e.AppliedAt });
            entity.HasIndex(e => e.AppliedAt);

            entity.HasOne(e => e.SourceProduct)
                .WithMany()
                .HasForeignKey(e => e.SourceProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetProduct)
                .WithMany()
                .HasForeignKey(e => e.TargetProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransformationYieldProfile>(entity =>
        {
            entity.HasIndex(e => new { e.StoreId, e.SupplierId, e.SourceProductId, e.TargetProductId }).IsUnique();
            entity.HasIndex(e => e.LastRecalculatedAt);

            entity.HasOne(e => e.SourceProduct)
                .WithMany()
                .HasForeignKey(e => e.SourceProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetProduct)
                .WithMany()
                .HasForeignKey(e => e.TargetProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransformationYieldRecalibrationLog>(entity =>
        {
            entity.HasIndex(e => new { e.StoreId, e.SupplierId, e.SourceProductId, e.TargetProductId, e.Status });
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.SourceProduct)
                .WithMany()
                .HasForeignKey(e => e.SourceProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetProduct)
                .WithMany()
                .HasForeignKey(e => e.TargetProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CigaretteCount>(entity =>
        {
            entity.HasIndex(e => e.CashSessionId).IsUnique();
            entity.HasIndex(e => e.CountDate);

            entity.HasOne(cc => cc.CashSession)
                .WithOne(cs => cs.CigaretteCount)
                .HasForeignKey<CigaretteCount>(cc => cc.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CigaretteCountLine>(entity =>
        {
            entity.HasIndex(e => e.CigaretteCountId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(ccl => ccl.CigaretteCount)
                .WithMany(cc => cc.Lines)
                .HasForeignKey(ccl => ccl.CigaretteCountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ccl => ccl.Product)
                .WithMany()
                .HasForeignKey(ccl => ccl.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TimePunch>(entity =>
        {
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.CashSessionId);
            entity.HasIndex(e => e.PunchTime);
            entity.HasIndex(e => new { e.UsuarioId, e.PunchType });
            entity.HasIndex(e => e.UsuarioId)
                .HasFilter("\"IsOpen\" = true")
                .IsUnique();

            entity.HasOne(tp => tp.Usuario)
                .WithMany()
                .HasForeignKey(tp => tp.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.Device)
                .WithMany()
                .HasForeignKey(tp => tp.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(tp => tp.CashSession)
                .WithMany()
                .HasForeignKey(tp => tp.CashSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(tp => tp.OperatorSession)
                .WithMany()
                .HasForeignKey(tp => tp.OperatorSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(tp => tp.AdjustedBy)
                .WithMany()
                .HasForeignKey(tp => tp.AdjustedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TimePunchAdjustment>(entity =>
        {
            entity.HasIndex(e => e.TimePunchId);
            entity.HasIndex(e => e.AdjustedById);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(tpa => tpa.TimePunch)
                .WithMany()
                .HasForeignKey(tpa => tpa.TimePunchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tpa => tpa.AdjustedBy)
                .WithMany()
                .HasForeignKey(tpa => tpa.AdjustedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeExtra>(entity =>
        {
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.ExtraDate);
            entity.HasIndex(e => new { e.UsuarioId, e.Year, e.Month });

            entity.HasOne(ee => ee.Usuario)
                .WithMany()
                .HasForeignKey(ee => ee.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ee => ee.CreatedBy)
                .WithMany()
                .HasForeignKey(ee => ee.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ee => ee.ApprovedBy)
                .WithMany()
                .HasForeignKey(ee => ee.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PayrollReceipt>(entity =>
        {
            entity.HasIndex(e => new { e.UsuarioId, e.Year, e.Month }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(pr => pr.Usuario)
                .WithMany()
                .HasForeignKey(pr => pr.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KanbanBoard>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<KanbanTask>(entity =>
        {
            entity.HasIndex(e => e.CashSessionId);
            entity.HasIndex(e => e.KanbanBoardId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.CashSessionId, e.IsRequiredForShiftClose, e.Status });

            entity.HasOne(e => e.KanbanBoard)
                .WithMany(b => b.Tasks)
                .HasForeignKey(e => e.KanbanBoardId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CashSession)
                .WithMany()
                .HasForeignKey(e => e.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.KanbanTemplate)
                .WithMany()
                .HasForeignKey(e => e.KanbanTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<KanbanChecklistItem>(entity =>
        {
            entity.HasIndex(e => e.KanbanTaskId);

            entity.HasOne(e => e.KanbanTask)
                .WithMany(t => t.ChecklistItems)
                .HasForeignKey(e => e.KanbanTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KanbanComment>(entity =>
        {
            entity.HasIndex(e => e.KanbanTaskId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.KanbanTask)
                .WithMany(t => t.Comments)
                .HasForeignKey(e => e.KanbanTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KanbanTemplate>(entity =>
        {
            entity.HasIndex(e => e.KanbanBoardId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Shift);

            entity.HasOne(e => e.KanbanBoard)
                .WithMany(b => b.Templates)
                .HasForeignKey(e => e.KanbanBoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateChecklistItem>(entity =>
        {
            entity.HasIndex(e => e.KanbanTemplateId);

            entity.HasOne(e => e.KanbanTemplate)
                .WithMany(t => t.ChecklistItems)
                .HasForeignKey(e => e.KanbanTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurrenceRule>(entity =>
        {
            entity.HasIndex(e => e.KanbanTemplateId);
            entity.HasIndex(e => e.Frequency);

            entity.HasOne(e => e.KanbanTemplate)
                .WithMany(t => t.RecurrenceRules)
                .HasForeignKey(e => e.KanbanTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GeneratedTask>(entity =>
        {
            entity.HasIndex(e => new { e.CashSessionId, e.GenerationDate, e.Shift, e.KanbanTemplateId }).IsUnique();
            entity.HasIndex(e => e.KanbanTaskId).IsUnique();

            entity.HasOne(e => e.CashSession)
                .WithMany()
                .HasForeignKey(e => e.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.KanbanTemplate)
                .WithMany()
                .HasForeignKey(e => e.KanbanTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.KanbanTask)
                .WithMany()
                .HasForeignKey(e => e.KanbanTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportJob>(entity =>
        {
            entity.HasIndex(e => e.ImportType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Mode);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<ImportJobError>(entity =>
        {
            entity.HasIndex(e => e.ImportJobId);
            entity.HasIndex(e => e.RowNumber);

            entity.HasOne(e => e.ImportJob)
                .WithMany(j => j.Errors)
                .HasForeignKey(e => e.ImportJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OnboardingSession>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CreatedByUsuarioId);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OnboardingStepState>(entity =>
        {
            entity.HasIndex(e => e.OnboardingSessionId);
            entity.HasIndex(e => new { e.OnboardingSessionId, e.StepKey }).IsUnique();

            entity.HasOne(e => e.OnboardingSession)
                .WithMany(s => s.Steps)
                .HasForeignKey(e => e.OnboardingSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrainingChecklist>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Role, e.IsActive });
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrainingChecklistItem>(entity =>
        {
            entity.HasIndex(e => new { e.TrainingChecklistId, e.SortOrder });

            entity.HasOne(e => e.TrainingChecklist)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.TrainingChecklistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrainingChecklistRun>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Role, e.StartedAt });
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TrainingChecklist)
                .WithMany()
                .HasForeignKey(e => e.TrainingChecklistId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.StartedByUsuario)
                .WithMany()
                .HasForeignKey(e => e.StartedByUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TrainingChecklistRunItem>(entity =>
        {
            entity.HasIndex(e => new { e.TrainingChecklistRunId, e.TrainingChecklistItemId }).IsUnique();

            entity.HasOne(e => e.TrainingChecklistRun)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.TrainingChecklistRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TrainingChecklistItem)
                .WithMany()
                .HasForeignKey(e => e.TrainingChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
