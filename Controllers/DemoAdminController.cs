using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/admin/demo")]
public class DemoAdminController : ControllerBase
{
    private const string DemoTenantCode = "demo-tenant";
    private const string DemoStoreCode = "demo-store";

    private readonly ApplicationDbContext _db;
    private readonly IHashService _hashService;
    private readonly ITrainingService _trainingService;

    public DemoAdminController(ApplicationDbContext db, IHashService hashService, ITrainingService trainingService)
    {
        _db = db;
        _hashService = hashService;
        _trainingService = trainingService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> Status()
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Code == DemoTenantCode);
        if (tenant == null)
        {
            return Ok(new
            {
                exists = false,
                lastSeedAt = await GetSettingAsync("demo:last_seed_at"),
                lastResetAt = await GetSettingAsync("demo:last_reset_at")
            });
        }

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.TenantId == tenant.Id && s.Code == DemoStoreCode);
        return Ok(new
        {
            exists = true,
            tenantId = tenant.Id,
            storeId = store?.Id,
            users = await _db.Usuarios.CountAsync(u => u.TenantId == tenant.Id),
            products = await _db.Products.CountAsync(p => p.Name.StartsWith("Demo ")),
            sales = store == null ? 0 : await _db.Sales.CountAsync(s => s.StoreId == store.Id),
            lastSeedAt = await GetSettingAsync("demo:last_seed_at"),
            lastResetAt = await GetSettingAsync("demo:last_reset_at")
        });
    }

    [HttpPost("seed")]
    public async Task<ActionResult<object>> Seed()
    {
        await ResetAndSeedAsync(markReset: false);
        return await Status();
    }

    [HttpPost("reset")]
    public async Task<ActionResult<object>> Reset()
    {
        await ResetAndSeedAsync(markReset: true);
        return await Status();
    }

    [HttpPost("reset-training")]
    public async Task<ActionResult<object>> ResetTraining()
    {
        var demoTenantId = await _db.Tenants.Where(t => t.Code == DemoTenantCode).Select(t => (int?)t.Id).FirstOrDefaultAsync();
        var targetTenantId = await _trainingService.ResetTrainingAsync(demoTenantId);
        if (!targetTenantId.HasValue)
            return NotFound(new { message = "No se encontro tenant para entrenamiento" });

        await UpsertSettingAsync("demo:last_training_reset_at", DateTime.UtcNow.ToString("O"));
        await _db.SaveChangesAsync();

        return Ok(new
        {
            tenantId = targetTenantId.Value,
            lastTrainingResetAt = await GetSettingAsync("demo:last_training_reset_at")
        });
    }

    private async Task ResetAndSeedAsync(bool markReset)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        await PurgeDemoAsync();

        var tenant = new Tenant { Name = "Demo Market", Code = DemoTenantCode, IsActive = true, IsTrainingTenant = true, CreatedAt = DateTime.UtcNow };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        var store = new Store
        {
            TenantId = tenant.Id,
            Name = "Demo Store Central",
            Code = DemoStoreCode,
            Address = "Av. Demo 123",
            Phone = "+54 11 5555-0000",
            IsActive = true,
            SettingsJson = "{}",
            CreatedAt = DateTime.UtcNow
        };
        _db.Stores.Add(store);

        _db.TenantBrandingSettings.Add(new TenantBrandingSettings
        {
            TenantId = tenant.Id,
            DisplayName = "Demo Market",
            LogoUrl = null,
            PrimaryColor = "#1e8a63",
            SecondaryColor = "#2a3a4f",
            TicketHeaderText = "Gracias por comprar en Demo Market",
            TicketFooterText = "Volva pronto",
            ReturnPolicyText = "Cambios dentro de 24h con ticket",
            SupportPhone = "+54 11 5555-0101",
            SupportEmail = "soporte@demomarket.local",
            UpdatedAt = DateTime.UtcNow
        });

        var admin = new Usuario { Username = "demo.admin", PasswordHash = _hashService.HashSha256("demo123"), PinHash = _hashService.HashSha256("1111"), Role = UserRole.Admin.ToString(), IsActive = true, TenantId = tenant.Id, CreatedAt = DateTime.UtcNow };
        var encargado = new Usuario { Username = "demo.encargado", PasswordHash = _hashService.HashSha256("demo123"), PinHash = _hashService.HashSha256("2222"), Role = UserRole.Supervisor.ToString(), IsActive = true, TenantId = tenant.Id, CreatedAt = DateTime.UtcNow };
        var caja = new Usuario { Username = "demo.caja", PasswordHash = _hashService.HashSha256("demo123"), PinHash = _hashService.HashSha256("3333"), Role = UserRole.Operator.ToString(), IsActive = true, TenantId = tenant.Id, CreatedAt = DateTime.UtcNow };
        var tablet = new Usuario { Username = "demo.tablet", PasswordHash = _hashService.HashSha256("demo123"), PinHash = _hashService.HashSha256("4444"), Role = UserRole.Operator.ToString(), IsActive = true, TenantId = tenant.Id, CreatedAt = DateTime.UtcNow };
        _db.Usuarios.AddRange(admin, encargado, caja, tablet);

        await _db.SaveChangesAsync();

        _db.StoreUsers.AddRange(new[]
        {
            new StoreUser { StoreId = store.Id, UsuarioId = admin.Id, Role = admin.Role, IsActive = true, CreatedAt = DateTime.UtcNow },
            new StoreUser { StoreId = store.Id, UsuarioId = encargado.Id, Role = encargado.Role, IsActive = true, CreatedAt = DateTime.UtcNow },
            new StoreUser { StoreId = store.Id, UsuarioId = caja.Id, Role = caja.Role, IsActive = true, CreatedAt = DateTime.UtcNow },
            new StoreUser { StoreId = store.Id, UsuarioId = tablet.Id, Role = tablet.Role, IsActive = true, CreatedAt = DateTime.UtcNow }
        });

        var cajaDevice = new Device { UsuarioId = caja.Id, StoreId = store.Id, TokenHash = _hashService.HashSha256("demo-device-caja"), DeviceName = "Demo Caja", DeviceType = "CashRegister", CreatedAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        var tabletDevice = new Device { UsuarioId = tablet.Id, StoreId = store.Id, TokenHash = _hashService.HashSha256("demo-device-tablet"), DeviceName = "Demo Tablet", DeviceType = "Tablet", CreatedAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        _db.Devices.AddRange(cajaDevice, tabletDevice);
        await _db.SaveChangesAsync();

        var opSession = new OperatorSession
        {
            UsuarioId = caja.Id,
            SessionTokenHash = _hashService.HashSha256("demo-operator-session"),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(15),
            LastSeenAt = DateTime.UtcNow,
            IsRevoked = false
        };
        _db.OperatorSessions.Add(opSession);

        var envase = new ContainerType { Name = "Demo Botella 1L", DepositAmount = 300, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ContainerTypes.Add(envase);
        await _db.SaveChangesAsync();

        var pUnit = new Product { Name = "Demo Yerba 1kg", Barcode = "779000000001", QuickCode = "D001", SaleType = SaleType.Unit.ToString(), UnitName = "Unit", CatalogStatus = CatalogStatus.Active.ToString(), StockControl = true, DefaultPrice = 5200, LastCost = 3900, CreatedAt = DateTime.UtcNow };
        var pWeight = new Product { Name = "Demo Queso x Kg", Barcode = "279000000002", QuickCode = "D002", SaleType = SaleType.Weight.ToString(), UnitName = "kg", CatalogStatus = CatalogStatus.Active.ToString(), StockControl = true, DefaultPricePerKg = 9800, LastCost = 7600, CreatedAt = DateTime.UtcNow };
        var pCig = new Product { Name = "Demo Cigarrillos Box", Barcode = "779000000003", QuickCode = "D003", SaleType = SaleType.Unit.ToString(), IsCigarette = true, CatalogStatus = CatalogStatus.Active.ToString(), StockControl = true, DefaultPrice = 4200, LastCost = 3400, CreatedAt = DateTime.UtcNow };
        var pRet = new Product { Name = "Demo Gaseosa Retornable", Barcode = "779000000004", QuickCode = "D004", SaleType = SaleType.Unit.ToString(), ContainerTypeId = envase.Id, CatalogStatus = CatalogStatus.Active.ToString(), StockControl = true, DefaultPrice = 1800, LastCost = 1200, CreatedAt = DateTime.UtcNow };
        var pPending = new Product { Name = "Demo Producto Pendiente", Barcode = "779000000005", QuickCode = "D005", SaleType = SaleType.Unit.ToString(), CatalogStatus = CatalogStatus.Pending.ToString(), StockControl = false, DefaultPrice = 999, CreatedAt = DateTime.UtcNow };
        _db.Products.AddRange(pUnit, pWeight, pCig, pRet, pPending);
        await _db.SaveChangesAsync();

        var priceList = await _db.PriceLists.FirstOrDefaultAsync(x => x.IsDefault);
        if (priceList == null)
        {
            priceList = new PriceList { Name = "General", IsDefault = true, IsActive = true, CreatedAt = DateTime.UtcNow };
            _db.PriceLists.Add(priceList);
            await _db.SaveChangesAsync();
        }

        _db.ProductPrices.AddRange(new[]
        {
            new ProductPrice { ProductId = pUnit.Id, PriceListId = priceList.Id, Price = pUnit.DefaultPrice, PricePerKg = 0, CreatedAt = DateTime.UtcNow },
            new ProductPrice { ProductId = pWeight.Id, PriceListId = priceList.Id, Price = 0, PricePerKg = pWeight.DefaultPricePerKg, CreatedAt = DateTime.UtcNow },
            new ProductPrice { ProductId = pCig.Id, PriceListId = priceList.Id, Price = pCig.DefaultPrice, PricePerKg = 0, CreatedAt = DateTime.UtcNow },
            new ProductPrice { ProductId = pRet.Id, PriceListId = priceList.Id, Price = pRet.DefaultPrice, PricePerKg = 0, CreatedAt = DateTime.UtcNow }
        });

        _db.ProductStocks.AddRange(new[]
        {
            new ProductStock { ProductId = pUnit.Id, StoreId = store.Id, Bucket = StockBucket.VENDIBLE.ToString(), Quantity = 40, UpdatedAt = DateTime.UtcNow },
            new ProductStock { ProductId = pWeight.Id, StoreId = store.Id, Bucket = StockBucket.VENDIBLE.ToString(), Quantity = 12, UpdatedAt = DateTime.UtcNow },
            new ProductStock { ProductId = pCig.Id, StoreId = store.Id, Bucket = StockBucket.VENDIBLE.ToString(), Quantity = 6, UpdatedAt = DateTime.UtcNow },
            new ProductStock { ProductId = pRet.Id, StoreId = store.Id, Bucket = StockBucket.VENDIBLE.ToString(), Quantity = 15, UpdatedAt = DateTime.UtcNow }
        });

        var cFiadoOk = new Customer { FullName = "Demo Cliente Fiado Activo", DNI = "30111222", AllowsCredit = true, CreditLimit = 50000, IsFixedCustomer = true, IsActive = true, CreatedAt = DateTime.UtcNow };
        var cFiadoNo = new Customer { FullName = "Demo Cliente Fiado Bloqueado", DNI = "30111333", AllowsCredit = false, CreditLimit = 0, IsFixedCustomer = true, IsActive = true, CreatedAt = DateTime.UtcNow };
        var cEnv = new Customer { FullName = "Demo Cliente Envases", DNI = "30111444", AllowsCredit = false, IsFixedCustomer = true, IsActive = true, CreatedAt = DateTime.UtcNow };
        var cAnon = new Customer { FullName = "Consumidor Final Demo", IsAnonymous = true, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Customers.AddRange(cFiadoOk, cFiadoNo, cEnv, cAnon);

        var provA = new Supplier { Name = "Demo Proveedor Bebidas", CUIT = "30-70000001-9", Phone = "1140001111", Email = "bebidas@demo.local", IsActive = true, CreatedAt = DateTime.UtcNow };
        var provB = new Supplier { Name = "Demo Proveedor Almacen", CUIT = "30-70000002-7", Phone = "1140002222", Email = "almacen@demo.local", IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Suppliers.AddRange(provA, provB);

        await _db.SaveChangesAsync();

        _db.CustomerAccountMovements.Add(new CustomerAccountMovement
        {
            CustomerId = cFiadoOk.Id,
            MovementType = MovementType.CreditPurchase.ToString(),
            ReferenceType = "Sale",
            Amount = 8500,
            Description = "Saldo fiado demo",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });

        _db.ContainerMovements.Add(new ContainerMovement
        {
            CustomerId = cEnv.Id,
            ContainerTypeId = envase.Id,
            Direction = ContainerDirection.Given.ToString(),
            Qty = 4,
            RefType = "Sale",
            CreatedByOperatorSessionId = opSession.Id,
            CreatedByUsuarioId = caja.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        var sessionClosed = new CashSession
        {
            DeviceId = cajaDevice.Id,
            StoreId = store.Id,
            OperatorSessionId = opSession.Id,
            Shift = Shift.Morning.ToString(),
            Status = CashSessionStatus.Closed.ToString(),
            OpeningCash = 30000,
            TotalCash = 25000,
            TotalCard = 10000,
            TotalTransfer = 4000,
            TotalCredit = 8500,
            DeclaredCash = 25000,
            DeclaredCard = 10000,
            DeclaredTransfer = 4000,
            DeclaredCredit = 8500,
            OpenedAt = DateTime.UtcNow.AddDays(-1).AddHours(-8),
            ClosedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1)
        };

        var sessionOpen = new CashSession
        {
            DeviceId = cajaDevice.Id,
            StoreId = store.Id,
            OperatorSessionId = opSession.Id,
            Shift = Shift.Afternoon.ToString(),
            Status = CashSessionStatus.Open.ToString(),
            OpeningCash = 28000,
            OpenedAt = DateTime.UtcNow.AddHours(-2)
        };
        _db.CashSessions.AddRange(sessionClosed, sessionOpen);
        await _db.SaveChangesAsync();

        var s1 = CreateSale(store.Id, cajaDevice.Id, opSession.Id, sessionClosed.Id, cFiadoOk.Id, DateTime.UtcNow.AddDays(-3), SaleStatus.Completed.ToString(), 5200, PaymentMethod.Cash.ToString(), PaymentStatus.Confirmed.ToString(), pUnit);
        var s2 = CreateSale(store.Id, cajaDevice.Id, opSession.Id, sessionClosed.Id, cFiadoNo.Id, DateTime.UtcNow.AddDays(-2), SaleStatus.Completed.ToString(), 9800, PaymentMethod.Card.ToString(), PaymentStatus.Confirmed.ToString(), pWeight);
        var s3 = CreateSale(store.Id, cajaDevice.Id, opSession.Id, sessionClosed.Id, cAnon.Id, DateTime.UtcNow.AddDays(-1), SaleStatus.Completed.ToString(), 4200, PaymentMethod.Cash.ToString(), PaymentStatus.Confirmed.ToString(), pCig);
        var s4 = CreateSale(store.Id, cajaDevice.Id, opSession.Id, sessionOpen.Id, cEnv.Id, DateTime.UtcNow.AddHours(-6), SaleStatus.Completed.ToString(), 1800, PaymentMethod.Cash.ToString(), PaymentStatus.Confirmed.ToString(), pRet);
        var pending = CreateSale(store.Id, cajaDevice.Id, opSession.Id, sessionOpen.Id, cFiadoOk.Id, DateTime.UtcNow.AddHours(-1), SaleStatus.PendingTransfer.ToString(), 6500, PaymentMethod.Transfer.ToString(), PaymentStatus.Pending.ToString(), pUnit);

        _db.Sales.AddRange(s1, s2, s3, s4, pending);
        await _db.SaveChangesAsync();

        var saleReturn = new SaleReturn
        {
            OriginalSaleId = s3.Id,
            CashSessionId = sessionOpen.Id,
            StoreId = store.Id,
            RefundPreference = RefundPreference.Cash.ToString(),
            RefundTotal = 2100,
            ReturnedSubtotal = 2100,
            ReturnedCigaretteSurchargeShare = 0,
            CustomerId = cAnon.Id,
            CustomerAlias = "Consumidor Final Demo",
            CreatedByOperatorSessionId = opSession.Id,
            CreatedByUsuarioId = caja.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-40)
        };
        _db.SaleReturns.Add(saleReturn);
        await _db.SaveChangesAsync();

        _db.SaleReturnLines.Add(new SaleReturnLine
        {
            SaleReturnId = saleReturn.Id,
            OriginalSaleItemId = s3.Items.First().Id,
            ProductId = s3.Items.First().ProductId,
            QtyReturned = 0.5m,
            Condition = ReturnCondition.Resellable.ToString(),
            LineRefundAmount = 2100,
            IsCigarette = true
        });

        var claim = new SupplierClaim
        {
            StoreId = store.Id,
            SupplierId = provA.Id,
            Status = SupplierClaimStatus.Pending.ToString(),
            HasReceipt = true,
            ReceiptType = "Factura",
            ReceiptNumber = "A-0001-000123",
            Notes = "Demo: mercaderia con dano",
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        };
        _db.SupplierClaims.Add(claim);
        await _db.SaveChangesAsync();

        _db.SupplierClaimItems.Add(new SupplierClaimItem { SupplierClaimId = claim.Id, ProductId = pRet.Id, Quantity = 2, UnitCostSnapshot = 1200, Notes = "Botellas golpeadas" });
        _db.SupplierCredits.Add(new SupplierCredit { SupplierId = provA.Id, SupplierClaimId = claim.Id, Amount = 2400, RemainingAmount = 2400, Notes = "Credito demo", CreatedAt = DateTime.UtcNow.AddHours(-4) });

        _db.TimePunches.Add(new TimePunch
        {
            UsuarioId = caja.Id,
            DeviceId = cajaDevice.Id,
            CashSessionId = sessionOpen.Id,
            OperatorSessionId = opSession.Id,
            PunchType = TimePunchType.Entry.ToString(),
            IsOpen = true,
            PunchTime = DateTime.UtcNow.AddHours(-9),
            CreatedAt = DateTime.UtcNow.AddHours(-9)
        });

        _db.EmployeeExtras.Add(new EmployeeExtra
        {
            UsuarioId = tablet.Id,
            CreatedById = encargado.Id,
            ExtraDate = DateTime.UtcNow.Date.AddDays(-1),
            Year = DateTime.UtcNow.Year,
            Month = DateTime.UtcNow.Month,
            Hours = 2,
            Reason = "Cobertura demo",
            IsApproved = false,
            CreatedAt = DateTime.UtcNow.AddHours(-10)
        });

        var board = new KanbanBoard { Name = "Demo Turno", Description = "Checklist demo comercial", IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.KanbanBoards.Add(board);
        await _db.SaveChangesAsync();

        _db.KanbanTasks.Add(new KanbanTask
        {
            KanbanBoardId = board.Id,
            CashSessionId = sessionOpen.Id,
            StoreId = store.Id,
            Title = "Demo - Control de vencimientos",
            Description = "Tarea requerida para cierre comercial",
            Status = KanbanTaskStatus.Pending.ToString(),
            IsRequiredForShiftClose = true,
            AssignedToUsuarioId = encargado.Id,
            UpdatedByUsuarioId = encargado.Id,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        });

        _db.StockMovements.Add(new StockMovement
        {
            ProductId = pWeight.Id,
            StoreId = store.Id,
            Bucket = StockBucket.MERMA.ToString(),
            DeltaQty = -0.75m,
            MovementType = StockMovementType.Waste.ToString(),
            DeviceId = cajaDevice.Id,
            OperatorSessionId = opSession.Id,
            Notes = "Merma demo",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        await UpsertSettingAsync("demo:last_seed_at", DateTime.UtcNow.ToString("O"));
        if (markReset)
            await UpsertSettingAsync("demo:last_reset_at", DateTime.UtcNow.ToString("O"));

        await _trainingService.EnsureSeededAsync(tenant.Id);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private Sale CreateSale(int storeId, int deviceId, int operatorSessionId, int cashSessionId, int customerId, DateTime createdAt, string status, decimal total, string method, string paymentStatus, Product product)
    {
        return new Sale
        {
            CartId = null,
            DeviceId = deviceId,
            StoreId = storeId,
            OperatorSessionId = operatorSessionId,
            CashSessionId = cashSessionId,
            CustomerId = customerId,
            Status = status,
            Subtotal = total,
            Total = total,
            CreatedAt = createdAt,
            CompletedAt = status == SaleStatus.Completed.ToString() ? createdAt.AddMinutes(10) : null,
            ExternalTicketId = $"DEMO-{Guid.NewGuid():N}"[..24],
            Items = new List<SaleItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductCode = product.Barcode ?? product.QuickCode ?? product.Id.ToString(),
                    ProductName = product.Name,
                    UnitPrice = product.SaleType == SaleType.Weight.ToString() ? product.DefaultPricePerKg : product.DefaultPrice,
                    Quantity = product.SaleType == SaleType.Weight.ToString() ? 1m : 1m,
                    Unit = product.SaleType == SaleType.Weight.ToString() ? MeasureUnit.Weight.ToString() : MeasureUnit.Unit.ToString(),
                    ContainerTypeId = product.ContainerTypeId,
                    ContainerOwedQty = product.ContainerTypeId.HasValue ? 1 : 0,
                    ContainerDepositAmountSnapshot = product.ContainerTypeId.HasValue ? 300 : 0
                }
            },
            Payments = new List<SalePayment>
            {
                new()
                {
                    PaymentMethod = method,
                    Status = paymentStatus,
                    Amount = total,
                    Reference = method == PaymentMethod.Transfer.ToString() ? "TRX-DEMO" : null,
                    CreatedAt = createdAt
                }
            }
        };
    }

    private async Task PurgeDemoAsync()
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Code == DemoTenantCode);
        if (tenant == null)
            return;

        var storeIds = await _db.Stores.Where(s => s.TenantId == tenant.Id).Select(s => s.Id).ToListAsync();
        var userIds = await _db.Usuarios.Where(u => u.TenantId == tenant.Id).Select(u => u.Id).ToListAsync();
        var deviceIds = await _db.Devices.Where(d => userIds.Contains(d.UsuarioId) || (d.StoreId.HasValue && storeIds.Contains(d.StoreId.Value))).Select(d => d.Id).ToListAsync();
        var supplierIds = await _db.Suppliers.Where(s => s.Name.StartsWith("Demo ")).Select(s => s.Id).ToListAsync();
        var demoProductIds = await _db.Products.Where(p => p.Name.StartsWith("Demo ")).Select(p => p.Id).ToListAsync();
        var saleIds = await _db.Sales.Where(s => storeIds.Contains(s.StoreId ?? -1) || deviceIds.Contains(s.DeviceId)).Select(s => s.Id).ToListAsync();
        var claimIds = await _db.SupplierClaims.Where(c => storeIds.Contains(c.StoreId ?? -1)).Select(c => c.Id).ToListAsync();
        var purchaseIds = await _db.Purchases
            .Where(p =>
                (p.StoreId.HasValue && storeIds.Contains(p.StoreId.Value))
                || deviceIds.Contains(p.DeviceId)
                || (p.SupplierId.HasValue && supplierIds.Contains(p.SupplierId.Value)))
            .Select(p => p.Id)
            .ToListAsync();
        var supplierReturnIds = await _db.SupplierReturns
            .Where(r => supplierIds.Contains(r.SupplierId) || deviceIds.Contains(r.DeviceId) || userIds.Contains(r.CreatedByUsuarioId))
            .Select(r => r.Id)
            .ToListAsync();
        var externalExchangeIds = await _db.ExternalExchanges
            .Where(e => supplierIds.Contains(e.SupplierId) || deviceIds.Contains(e.DeviceId) || userIds.Contains(e.CreatedByUsuarioId))
            .Select(e => e.Id)
            .ToListAsync();
        var suggestionIds = await _db.PurchaseSuggestions
            .Where(s =>
                (s.StoreId.HasValue && storeIds.Contains(s.StoreId.Value))
                || s.TenantId == tenant.Id
                || (s.GeneratedByUsuarioId.HasValue && userIds.Contains(s.GeneratedByUsuarioId.Value)))
            .Select(s => s.Id)
            .ToListAsync();

        if (saleIds.Count > 0)
        {
            var returnIds = await _db.SaleReturns.Where(r => saleIds.Contains(r.OriginalSaleId)).Select(r => r.Id).ToListAsync();
            await _db.SaleReturnLines.Where(x => returnIds.Contains(x.SaleReturnId)).ExecuteDeleteAsync();
            await _db.SaleReturns.Where(x => returnIds.Contains(x.Id)).ExecuteDeleteAsync();
            await _db.SalePayments.Where(x => saleIds.Contains(x.SaleId)).ExecuteDeleteAsync();
            await _db.SaleItems.Where(x => saleIds.Contains(x.SaleId)).ExecuteDeleteAsync();
            await _db.Sales.Where(x => saleIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (claimIds.Count > 0)
        {
            await _db.SupplierClaimItems.Where(x => claimIds.Contains(x.SupplierClaimId)).ExecuteDeleteAsync();
            await _db.SupplierCredits.Where(x => x.SupplierClaimId.HasValue && claimIds.Contains(x.SupplierClaimId.Value)).ExecuteDeleteAsync();
            await _db.SupplierClaims.Where(x => claimIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (suggestionIds.Count > 0 || demoProductIds.Count > 0 || supplierIds.Count > 0)
        {
            await _db.PurchaseSuggestionLines
                .Where(x =>
                    suggestionIds.Contains(x.PurchaseSuggestionId)
                    || demoProductIds.Contains(x.ProductId)
                    || (x.SuggestedSupplierId.HasValue && supplierIds.Contains(x.SuggestedSupplierId.Value)))
                .ExecuteDeleteAsync();
            await _db.PurchaseSuggestions.Where(x => suggestionIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (purchaseIds.Count > 0 || demoProductIds.Count > 0)
        {
            await _db.PurchaseItems
                .Where(x => purchaseIds.Contains(x.PurchaseId) || demoProductIds.Contains(x.ProductId))
                .ExecuteDeleteAsync();
            await _db.Purchases.Where(x => purchaseIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (supplierReturnIds.Count > 0 || demoProductIds.Count > 0)
        {
            await _db.SupplierReturnLines
                .Where(x => supplierReturnIds.Contains(x.SupplierReturnId) || demoProductIds.Contains(x.ProductId))
                .ExecuteDeleteAsync();
            await _db.SupplierReturns.Where(x => supplierReturnIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (externalExchangeIds.Count > 0 || demoProductIds.Count > 0)
        {
            await _db.ExternalExchangeLines
                .Where(x => externalExchangeIds.Contains(x.ExternalExchangeId) || demoProductIds.Contains(x.ProductId))
                .ExecuteDeleteAsync();
            await _db.ExternalExchanges.Where(x => externalExchangeIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (supplierIds.Count > 0 || demoProductIds.Count > 0)
        {
            await _db.TransformationYieldRecalibrationLogs
                .Where(x =>
                    (x.SupplierId.HasValue && supplierIds.Contains(x.SupplierId.Value))
                    || demoProductIds.Contains(x.SourceProductId)
                    || demoProductIds.Contains(x.TargetProductId))
                .ExecuteDeleteAsync();

            await _db.TransformationYieldProfiles
                .Where(x =>
                    (x.SupplierId.HasValue && supplierIds.Contains(x.SupplierId.Value))
                    || demoProductIds.Contains(x.SourceProductId)
                    || demoProductIds.Contains(x.TargetProductId))
                .ExecuteDeleteAsync();

            await _db.TransformationYieldEvents
                .Where(x =>
                    (x.SupplierId.HasValue && supplierIds.Contains(x.SupplierId.Value))
                    || demoProductIds.Contains(x.SourceProductId)
                    || demoProductIds.Contains(x.TargetProductId))
                .ExecuteDeleteAsync();
        }

        await _db.KanbanComments.Where(x => x.KanbanTask.Title.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.KanbanChecklistItems.Where(x => x.KanbanTask.Title.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.KanbanTasks.Where(x => x.Title.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.KanbanBoards.Where(x => x.Name.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.TimePunches.Where(x => userIds.Contains(x.UsuarioId)).ExecuteDeleteAsync();
        await _db.EmployeeExtras.Where(x => userIds.Contains(x.UsuarioId) || userIds.Contains(x.CreatedById)).ExecuteDeleteAsync();
        await _db.CustomerAccountMovements.Where(x => x.Customer.FullName.StartsWith("Demo ") || x.Customer.FullName.StartsWith("Consumidor Final Demo")).ExecuteDeleteAsync();
        await _db.ContainerMovements.Where(x => x.Customer.FullName.StartsWith("Demo ") || x.Customer.FullName.StartsWith("Consumidor Final Demo")).ExecuteDeleteAsync();
        await _db.Customers.Where(x => x.FullName.StartsWith("Demo ") || x.FullName.StartsWith("Consumidor Final Demo")).ExecuteDeleteAsync();
        await _db.StockMovements.Where(x => x.Product.Name.StartsWith("Demo ") || (x.StoreId.HasValue && storeIds.Contains(x.StoreId.Value))).ExecuteDeleteAsync();
        await _db.ProductStocks.Where(x => x.Product.Name.StartsWith("Demo ") || (x.StoreId.HasValue && storeIds.Contains(x.StoreId.Value))).ExecuteDeleteAsync();
        await _db.ProductPrices.Where(x => x.Product.Name.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.CartItems.Where(x => deviceIds.Contains(x.Cart.DeviceId)).ExecuteDeleteAsync();
        await _db.Carts.Where(x => deviceIds.Contains(x.DeviceId) || (x.TargetCashRegisterDeviceId.HasValue && deviceIds.Contains(x.TargetCashRegisterDeviceId.Value))).ExecuteDeleteAsync();
        await _db.Products.Where(x => x.Name.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.ContainerTypes.Where(x => x.Name.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.Suppliers.Where(x => x.Name.StartsWith("Demo ")).ExecuteDeleteAsync();
        await _db.StoreUsers.Where(x => storeIds.Contains(x.StoreId) || userIds.Contains(x.UsuarioId)).ExecuteDeleteAsync();
        await _db.Devices.Where(x => deviceIds.Contains(x.Id)).ExecuteDeleteAsync();
        await _db.OperatorSessions.Where(x => userIds.Contains(x.UsuarioId)).ExecuteDeleteAsync();
        await _db.Usuarios.Where(x => userIds.Contains(x.Id)).ExecuteDeleteAsync();
        await _db.TenantBrandingSettings.Where(x => x.TenantId == tenant.Id).ExecuteDeleteAsync();
        var trainingChecklistIds = await _db.TrainingChecklists.Where(x => x.TenantId == tenant.Id).Select(x => x.Id).ToListAsync();
        var trainingRunIds = await _db.TrainingChecklistRuns.Where(x => x.TenantId == tenant.Id).Select(x => x.Id).ToListAsync();
        if (trainingRunIds.Count > 0)
            await _db.TrainingChecklistRunItems.Where(x => trainingRunIds.Contains(x.TrainingChecklistRunId)).ExecuteDeleteAsync();
        if (trainingChecklistIds.Count > 0)
            await _db.TrainingChecklistItems.Where(x => trainingChecklistIds.Contains(x.TrainingChecklistId)).ExecuteDeleteAsync();
        await _db.TrainingChecklistRuns.Where(x => x.TenantId == tenant.Id).ExecuteDeleteAsync();
        await _db.TrainingChecklists.Where(x => x.TenantId == tenant.Id).ExecuteDeleteAsync();
        await _db.Stores.Where(x => storeIds.Contains(x.Id)).ExecuteDeleteAsync();
        await _db.Tenants.Where(x => x.Id == tenant.Id).ExecuteDeleteAsync();
        await _db.Settings.Where(x => x.Key.StartsWith("demo:")).ExecuteDeleteAsync();
    }

    private async Task<string?> GetSettingAsync(string key)
    {
        return await _db.Settings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();
    }

    private async Task UpsertSettingAsync(string key, string value)
    {
        var existing = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (existing == null)
        {
            _db.Settings.Add(new Setting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
            return;
        }

        existing.Value = value;
        existing.UpdatedAt = DateTime.UtcNow;
    }
}
