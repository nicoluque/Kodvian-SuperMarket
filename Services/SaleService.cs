using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface ISaleService
{
    Task<Sale> CreateFromCartAsync(int cartId, int operatorSessionId, int currentDeviceId, int? cashSessionId, int? customerId, decimal discount, List<(string paymentMethod, decimal amount, string? reference, bool isPending)> payments);
    Task<Sale?> GetByIdAsync(int id);
    Task<List<Sale>> GetByDeviceAsync(int deviceId);
    Task<string> GenerateInvoiceNumber();
    Task<List<Sale>> GetPendingTransfersAsync(int? cashRegisterDeviceId = null, int? cashSessionId = null);
    Task<Sale> ConfirmTransferAsync(int saleId, int paymentId, string? reference, string? notes);
    Task<List<Sale>> GetReturnEligibleSalesAsync(int? storeId, int hours = 48, string? query = null);
    Task<SaleReturn> CreateReturnAsync(int saleId, int cashSessionId, int operatorSessionId, int usuarioId, string refundPreference, int? customerId, string? customerAlias, List<(int originalSaleItemId, decimal qtyReturned, string condition)> lines);
    Task<List<SaleReturn>> GetReturnsAsync(int? saleId = null);
    Task<SaleReturn?> GetReturnByIdAsync(int returnId);
    Task<Sale> CancelPendingTransferAsync(int saleId, int operadorUsuarioId, string reason);
    Task<Sale> ImportManualSaleAsync(int cashSessionId, int operatorSessionId, int usuarioId, string externalTicketId, DateTime originalCreatedAt, string? customerAlias, List<(string code, decimal quantity, decimal? unitPrice)> items);
    Task<int> AssignQueuedSalesToCashSessionAsync(int cashSessionId, int cashRegisterDeviceId, int openingOperatorSessionId);
}

public class SaleService : ISaleService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly IStockService _stockService;
    private readonly IProductLookupService _productLookupService;
    private readonly ICustomerService _customerService;
    private readonly IConfiguration _config;

    public SaleService(ApplicationDbContext context, ICartService cartService, IStockService stockService, IProductLookupService productLookupService, ICustomerService customerService, IConfiguration config)
    {
        _context = context;
        _cartService = cartService;
        _stockService = stockService;
        _productLookupService = productLookupService;
        _customerService = customerService;
        _config = config;
    }

    public async Task<Sale> CreateFromCartAsync(int cartId, int operatorSessionId, int currentDeviceId, int? cashSessionId, int? customerId, decimal discount, List<(string paymentMethod, decimal amount, string? reference, bool isPending)> payments)
    {
        var existingSale = await _context.Sales
            .FirstOrDefaultAsync(s => s.CartId == cartId);

        if (existingSale != null)
            return existingSale;

        var hasPendingTransfer = payments.Any(p => p.paymentMethod == PaymentMethod.Transfer.ToString() && p.isPending);
        var useWebhookOnly = !bool.TryParse(_config["MercadoPago:MercadoPagoUseWebhookConfirmationOnly"], out var configured) || configured;
        var hasPendingQr = useWebhookOnly && payments.Any(p => p.paymentMethod == PaymentMethod.QrMp.ToString());
        var queueUntilCashOpen = !cashSessionId.HasValue;

        if (hasPendingTransfer && customerId == null)
            throw new InvalidOperationException("Debes seleccionar cliente cuando hay una transferencia pendiente");

        if (customerId.HasValue)
        {
            var existingPendingTransfer = await _context.Sales
                .FirstOrDefaultAsync(s => s.CustomerId == customerId.Value && s.Status == SaleStatus.PendingTransfer.ToString());

            if (existingPendingTransfer != null)
                throw new InvalidOperationException($"El cliente ya tiene una transferencia pendiente (Venta #{existingPendingTransfer.Id}). No se puede crear otra.");
        }

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
            throw new InvalidOperationException("Carrito no encontrado");

        var isCartOpenInSameDevice = cart.Status == CartStatus.Open.ToString() && cart.DeviceId == currentDeviceId;
        if (cart.Status != CartStatus.SentToCashier.ToString() && !isCartOpenInSameDevice)
            throw new InvalidOperationException("Primero debes enviar el carrito a caja");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var operatorSession = await _context.OperatorSessions.FindAsync(operatorSessionId);
            var operadorUsuarioId = operatorSession?.UsuarioId;

            var subtotal = cart.Items.Sum(i => i.Subtotal);
            var tax = 0m;
            var total = subtotal - discount + tax;

            string? invoiceNumber = null;
            DateTime? completedAt = null;
            var saleStatus = SaleStatus.Completed.ToString();

            if (queueUntilCashOpen)
            {
                saleStatus = SaleStatus.Pending.ToString();
            }
            else if (hasPendingTransfer)
            {
                saleStatus = SaleStatus.PendingTransfer.ToString();
            }
            else if (hasPendingQr)
            {
                saleStatus = SaleStatus.Pending.ToString();
            }
            else
            {
                invoiceNumber = await GenerateInvoiceNumber();
                completedAt = DateTime.UtcNow;
            }

            var sale = new Sale
            {
                CartId = cartId,
                DeviceId = cart.DeviceId,
                StoreId = cart.StoreId,
                OperatorSessionId = operatorSessionId,
                CashSessionId = cashSessionId,
                CustomerId = customerId,
                Status = saleStatus,
                Subtotal = subtotal,
                Discount = discount,
                Tax = tax,
                Total = total,
                InvoiceNumber = invoiceNumber,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = completedAt
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            foreach (var item in cart.Items)
            {
                Product? product = null;
                ContainerType? containerType = null;
                decimal containerDeposit = 0;
                decimal owedQty = 0;

                if (item.ProductId.HasValue)
                {
                    product = await _context.Products.FindAsync(item.ProductId.Value);
                    if (product?.ContainerTypeId != null && product.SaleType == SaleType.Unit.ToString())
                    {
                        if (item.ContainerReturnedNowQty < 0 || item.ContainerReturnedNowQty > item.Quantity)
                            throw new InvalidOperationException($"Invalid containerReturnedNowQty for item {item.ProductCode}");

                        containerType = await _context.ContainerTypes.FindAsync(product.ContainerTypeId.Value);
                        if (containerType == null)
                            throw new InvalidOperationException($"ContainerType not found for product {item.ProductCode}");

                        containerDeposit = product.ContainerDepositOverride ?? containerType.DepositAmount;
                        owedQty = item.Quantity - item.ContainerReturnedNowQty;

                        if (!customerId.HasValue)
                            owedQty = 0;
                    }
                }

                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Discount = item.Discount,
                    ContainerTypeId = containerType?.Id,
                    ContainerOwedQty = owedQty,
                    ContainerDepositAmountSnapshot = containerDeposit
                };
                _context.SaleItems.Add(saleItem);

                if (containerType != null && customerId.HasValue)
                {
                    if (owedQty > 0)
                    {
                        _context.ContainerMovements.Add(new ContainerMovement
                        {
                            CustomerId = customerId.Value,
                            ContainerTypeId = containerType.Id,
                            Direction = ContainerDirection.Given.ToString(),
                            Qty = owedQty,
                            RefType = "Sale",
                            RefId = sale.Id,
                            CreatedByOperatorSessionId = operatorSessionId,
                            CreatedByUsuarioId = operadorUsuarioId,
                            CreatedAt = DateTime.UtcNow
                        });

                        _context.CustomerAccountMovements.Add(new CustomerAccountMovement
                        {
                            CustomerId = customerId.Value,
                            MovementType = MovementType.ContainerCharge.ToString(),
                            ReferenceType = "Sale",
                            ReferenceId = sale.Id,
                            Amount = Math.Round(owedQty * containerDeposit, 2, MidpointRounding.AwayFromZero),
                            Description = $"Container charge from sale #{sale.Id}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (item.ContainerReturnedNowQty > 0)
                    {
                        _context.ContainerMovements.Add(new ContainerMovement
                        {
                            CustomerId = customerId.Value,
                            ContainerTypeId = containerType.Id,
                            Direction = ContainerDirection.Returned.ToString(),
                            Qty = item.ContainerReturnedNowQty,
                            RefType = "Sale",
                            RefId = sale.Id,
                            CreatedByOperatorSessionId = operatorSessionId,
                            CreatedByUsuarioId = operadorUsuarioId,
                            CreatedAt = DateTime.UtcNow
                        });

                        _context.CustomerAccountMovements.Add(new CustomerAccountMovement
                        {
                            CustomerId = customerId.Value,
                            MovementType = MovementType.ContainerRefund.ToString(),
                            ReferenceType = "Sale",
                            ReferenceId = sale.Id,
                            Amount = -Math.Round(item.ContainerReturnedNowQty * containerDeposit, 2, MidpointRounding.AwayFromZero),
                            Description = $"Container refund from sale #{sale.Id}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (item.ProductId.HasValue)
                {
                    if (product != null && product.StockControl)
                    {
                        await _stockService.ApplyMovementAsync(
                            item.ProductId.Value,
                            StockBucket.VENDIBLE.ToString(),
                            -item.Quantity,
                            StockMovementType.Sale.ToString(),
                            saleId: sale.Id,
                            deviceId: sale.DeviceId,
                            notes: $"Sale #{sale.Id}"
                        );
                    }
                }
            }

            foreach (var payment in payments)
            {
                var paymentStatus = PaymentStatus.Confirmed.ToString();
                if (queueUntilCashOpen
                    || (payment.paymentMethod == PaymentMethod.Transfer.ToString() && payment.isPending)
                    || (hasPendingQr && payment.paymentMethod == PaymentMethod.QrMp.ToString()))
                    paymentStatus = PaymentStatus.Pending.ToString();

                var salePayment = new SalePayment
                {
                    SaleId = sale.Id,
                    PaymentMethod = payment.paymentMethod,
                    Status = paymentStatus,
                    Amount = payment.amount,
                    Reference = payment.reference,
                    Provider = payment.paymentMethod == PaymentMethod.QrMp.ToString() ? "MercadoPago" : null,
                    ExternalReference = payment.paymentMethod == PaymentMethod.QrMp.ToString() ? sale.Id.ToString() : null,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SalePayments.Add(salePayment);
            }

            if (!queueUntilCashOpen && !hasPendingTransfer && !hasPendingQr)
            {
                var paymentTotal = payments.Sum(p => p.amount);
                if (paymentTotal < total)
                    throw new InvalidOperationException("El total de pagos es menor al total de la venta");
                    
            }

            cart.Status = CartStatus.Converted.ToString();
            cart.ConvertedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await _context.Sales
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .FirstAsync(s => s.Id == sale.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> AssignQueuedSalesToCashSessionAsync(int cashSessionId, int cashRegisterDeviceId, int openingOperatorSessionId)
    {
        var queuedSales = await _context.Sales
            .Include(s => s.Payments)
            .Include(s => s.Cart)
            .Where(s => s.CashSessionId == null
                        && s.CartId != null
                        && s.Cart != null
                        && s.Cart.TargetCashRegisterDeviceId == cashRegisterDeviceId
                        && s.Cart.Status == CartStatus.Converted.ToString()
                        && s.Status != SaleStatus.Voided.ToString()
                        && s.Status != SaleStatus.Cancelled.ToString())
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        if (!queuedSales.Any())
            return 0;

        var assignedCount = 0;

        foreach (var sale in queuedSales)
        {
            sale.CashSessionId = cashSessionId;
            sale.OperatorSessionId ??= openingOperatorSessionId;

            var hasPendingTransfer = false;
            var hasPendingQr = false;

            foreach (var payment in sale.Payments)
            {
                if (payment.Status != PaymentStatus.Pending.ToString())
                    continue;

                if (payment.PaymentMethod == PaymentMethod.Transfer.ToString())
                {
                    hasPendingTransfer = true;
                    continue;
                }

                if (payment.PaymentMethod == PaymentMethod.QrMp.ToString())
                {
                    hasPendingQr = true;
                    continue;
                }

                payment.Status = PaymentStatus.Confirmed.ToString();
                payment.ConfirmedAt ??= DateTime.UtcNow;
            }

            var hasAccountCreditConfirmed = sale.Payments.Any(p =>
                p.PaymentMethod == PaymentMethod.AccountCredit.ToString()
                && p.Status == PaymentStatus.Confirmed.ToString());

            if (hasAccountCreditConfirmed && sale.CustomerId.HasValue)
            {
                var alreadyRecorded = await _context.CustomerAccountMovements.AnyAsync(m =>
                    m.ReferenceType == "Sale"
                    && m.ReferenceId == sale.Id
                    && m.MovementType == MovementType.CreditPurchase.ToString());

                if (!alreadyRecorded)
                {
                    var amount = sale.Payments
                        .Where(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString() && p.Status == PaymentStatus.Confirmed.ToString())
                        .Sum(p => p.Amount);

                    if (amount > 0)
                        await _customerService.ProcessAccountCreditPaymentAsync(sale, sale.CustomerId.Value, amount);
                }
            }

            if (hasPendingTransfer)
            {
                sale.Status = SaleStatus.PendingTransfer.ToString();
            }
            else if (hasPendingQr)
            {
                sale.Status = SaleStatus.Pending.ToString();
            }
            else
            {
                sale.Status = SaleStatus.Completed.ToString();
                if (string.IsNullOrWhiteSpace(sale.InvoiceNumber))
                    sale.InvoiceNumber = await GenerateInvoiceNumber();
                sale.CompletedAt ??= DateTime.UtcNow;
            }

            assignedCount += 1;
        }

        await _context.SaveChangesAsync();
        return assignedCount;
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Sale>> GetByDeviceAsync(int deviceId)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Where(s => s.DeviceId == deviceId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Sale>> GetPendingTransfersAsync(int? cashRegisterDeviceId = null, int? cashSessionId = null)
    {
        var query = _context.Sales
            .Include(s => s.Payments)
            .Where(s => s.Status == SaleStatus.PendingTransfer.ToString());

        if (cashSessionId.HasValue)
        {
            query = query.Where(s => s.CashSessionId == cashSessionId.Value);
        }
        else if (cashRegisterDeviceId.HasValue)
        {
            query = query.Where(s => s.CashSessionId != null && s.CashSession!.DeviceId == cashRegisterDeviceId.Value);
        }

        return await query.OrderBy(s => s.CreatedAt).ToListAsync();
    }

    public async Task<Sale> ConfirmTransferAsync(int saleId, int paymentId, string? reference, string? notes)
    {
        var sale = await _context.Sales
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            throw new InvalidOperationException("Venta no encontrada");

        if (sale.Status != SaleStatus.PendingTransfer.ToString())
            throw new InvalidOperationException("Sale is not in PendingTransfer status");

        var payment = sale.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment == null)
            throw new InvalidOperationException("Pago no encontrado");

        if (payment.PaymentMethod != PaymentMethod.Transfer.ToString())
            throw new InvalidOperationException("Payment is not a transfer");

        if (payment.Status == PaymentStatus.Confirmed.ToString())
            throw new InvalidOperationException("Payment is already confirmed");

        payment.Status = PaymentStatus.Confirmed.ToString();
        if (!string.IsNullOrEmpty(reference))
            payment.Reference = reference;
        if (!string.IsNullOrEmpty(notes))
            payment.ConfirmNotes = notes;

        var confirmedPaymentsTotal = sale.Payments
            .Where(p => p.Status == PaymentStatus.Confirmed.ToString())
            .Sum(p => p.Amount);

        if (confirmedPaymentsTotal >= sale.Total)
        {
            sale.Status = SaleStatus.Paid.ToString();
            sale.InvoiceNumber = await GenerateInvoiceNumber();
            sale.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstAsync(s => s.Id == saleId);
    }

    public async Task<List<Sale>> GetReturnEligibleSalesAsync(int? storeId, int hours = 48, string? query = null)
    {
        var cutoff = DateTime.UtcNow.AddHours(-Math.Max(1, hours));

        var allowedStatuses = new[]
        {
            SaleStatus.Completed.ToString(),
            SaleStatus.Paid.ToString()
        };

        var salesQuery = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Items)
            .Where(s => s.CreatedAt >= cutoff && allowedStatuses.Contains(s.Status));

        if (storeId.HasValue)
            salesQuery = salesQuery.Where(s => s.StoreId == storeId.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            salesQuery = salesQuery.Where(s =>
                s.Id.ToString().Contains(q)
                || (s.Customer != null && s.Customer.FullName.ToLower().Contains(q))
                || (s.Customer != null && s.Customer.DNI != null && s.Customer.DNI.ToLower().Contains(q)));
        }

        return await salesQuery
            .OrderByDescending(s => s.CreatedAt)
            .Take(80)
            .ToListAsync();
    }

    public async Task<Sale> CancelPendingTransferAsync(int saleId, int operadorUsuarioId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Debes ingresar un motivo");

        var sale = await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            throw new InvalidOperationException("Venta no encontrada");

        if (sale.Status != SaleStatus.PendingTransfer.ToString())
            throw new InvalidOperationException("Only PendingTransfer sales can be cancelled");

        var hasConfirmedPayment = sale.Payments.Any(p => p.Status == PaymentStatus.Confirmed.ToString());
        if (hasConfirmedPayment)
            throw new InvalidOperationException("Cannot cancel pending transfer sale with confirmed payments");

        var hasInvalidPayment = sale.Payments.Any(p => p.PaymentMethod != PaymentMethod.Transfer.ToString());
        if (hasInvalidPayment)
            throw new InvalidOperationException("Pending transfer sale has non-transfer payments");

        foreach (var payment in sale.Payments.Where(p => p.PaymentMethod == PaymentMethod.Transfer.ToString() && p.Status == PaymentStatus.Pending.ToString()))
        {
            payment.Status = PaymentStatus.Rejected.ToString();
            payment.ConfirmNotes = reason;
        }

        foreach (var item in sale.Items)
        {
            if (!item.ProductId.HasValue)
                continue;

            var product = await _context.Products.FindAsync(item.ProductId.Value);
            if (product?.StockControl == true)
            {
                await _stockService.ApplyMovementAsync(
                    item.ProductId.Value,
                    StockBucket.VENDIBLE.ToString(),
                    item.Quantity,
                    "CANCEL_PENDING_TRANSFER_RESTOCK",
                    saleId: sale.Id,
                    operatorSessionId: sale.OperatorSessionId,
                    deviceId: sale.DeviceId,
                    notes: $"Pending transfer cancelled by user {operadorUsuarioId}: {reason}"
                );
            }
        }

        sale.Status = SaleStatus.Voided.ToString();
        await _context.SaveChangesAsync();

        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstAsync(s => s.Id == sale.Id);
    }

    public async Task<SaleReturn> CreateReturnAsync(int saleId, int cashSessionId, int operatorSessionId, int usuarioId, string refundPreference, int? customerId, string? customerAlias, List<(int originalSaleItemId, decimal qtyReturned, string condition)> lines)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            throw new InvalidOperationException("Venta no encontrada");

        if (DateTime.UtcNow > sale.CreatedAt.AddHours(48))
            throw new InvalidOperationException("Se excedió la ventana de devolución (48 horas)");

        if (lines.Count == 0)
            throw new InvalidOperationException("Debes seleccionar al menos una línea para devolver");

        if (refundPreference != Models.RefundPreference.Cash.ToString() && refundPreference != Models.RefundPreference.AccountCredit.ToString())
            throw new InvalidOperationException("Preferencia de devolución inválida");

        if (refundPreference == Models.RefundPreference.AccountCredit.ToString() && !customerId.HasValue)
            throw new InvalidOperationException("Debes indicar cliente para devolución a cuenta corriente");

        var session = await _context.CashSessions.FindAsync(cashSessionId);
        if (session == null || session.Status != CashSessionStatus.Open.ToString())
            throw new InvalidOperationException("La sesión de caja debe estar abierta");

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var returnedSubtotal = 0m;
            var returnedCigaretteQty = 0m;
            var totalSaleCigaretteQty = 0m;

            foreach (var item in sale.Items)
            {
                if (item.ProductId.HasValue)
                {
                    var p = await _context.Products.FindAsync(item.ProductId.Value);
                    if (p?.IsCigarette == true)
                        totalSaleCigaretteQty += item.Quantity;
                }
            }

            var saleReturn = new SaleReturn
            {
                OriginalSaleId = saleId,
                CashSessionId = cashSessionId,
                StoreId = sale.StoreId,
                RefundPreference = refundPreference,
                CustomerId = customerId,
                CustomerAlias = customerAlias,
                CreatedByOperatorSessionId = operatorSessionId,
                CreatedByUsuarioId = usuarioId,
                CreatedAt = DateTime.UtcNow
            };
            _context.SaleReturns.Add(saleReturn);
            await _context.SaveChangesAsync();

            foreach (var line in lines)
            {
                var originalItem = sale.Items.FirstOrDefault(i => i.Id == line.originalSaleItemId);
                if (originalItem == null)
                    throw new InvalidOperationException($"OriginalSaleItem {line.originalSaleItemId} not found in sale");

                if (line.qtyReturned <= 0)
                    throw new InvalidOperationException("Returned quantity must be > 0");

                var alreadyReturned = await _context.SaleReturnLines
                    .Where(r => r.OriginalSaleItemId == originalItem.Id)
                    .SumAsync(r => r.QtyReturned);

                var available = originalItem.Quantity - alreadyReturned;
                if (line.qtyReturned > available)
                    throw new InvalidOperationException($"Returned quantity exceeds available for item {originalItem.Id}");

                var lineFinalTotal = originalItem.Subtotal;
                var lineUnitRefund = originalItem.Quantity == 0 ? 0 : lineFinalTotal / originalItem.Quantity;
                var lineRefund = Math.Round(lineUnitRefund * line.qtyReturned, 2, MidpointRounding.AwayFromZero);

                bool isCigarette = false;
                if (originalItem.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(originalItem.ProductId.Value);
                    isCigarette = product?.IsCigarette == true;
                    if (product?.StockControl == true)
                    {
                        var bucket = line.condition == ReturnCondition.Waste.ToString()
                            ? StockBucket.MERMA.ToString()
                            : StockBucket.VENDIBLE.ToString();

                        await _stockService.ApplyMovementAsync(
                            originalItem.ProductId.Value,
                            bucket,
                            line.qtyReturned,
                            StockMovementType.Adjustment.ToString(),
                            saleId: sale.Id,
                            operatorSessionId: operatorSessionId,
                            deviceId: sale.DeviceId,
                            notes: $"Return from sale #{sale.Id}"
                        );
                    }
                }

                if (isCigarette)
                    returnedCigaretteQty += line.qtyReturned;

                returnedSubtotal += lineRefund;

                _context.SaleReturnLines.Add(new SaleReturnLine
                {
                    SaleReturnId = saleReturn.Id,
                    OriginalSaleItemId = originalItem.Id,
                    ProductId = originalItem.ProductId,
                    QtyReturned = line.qtyReturned,
                    Condition = line.condition,
                    LineRefundAmount = lineRefund,
                    IsCigarette = isCigarette
                });
            }

            var surchargeShare = 0m;
            if (sale.CigaretteSurcharge > 0 && returnedCigaretteQty > 0 && totalSaleCigaretteQty > 0)
            {
                surchargeShare = Math.Round(sale.CigaretteSurcharge * (returnedCigaretteQty / totalSaleCigaretteQty), 2, MidpointRounding.AwayFromZero);
            }

            var refundTotal = returnedSubtotal + surchargeShare;

            saleReturn.ReturnedSubtotal = returnedSubtotal;
            saleReturn.ReturnedCigaretteSurchargeShare = surchargeShare;
            saleReturn.RefundTotal = refundTotal;

            if (refundPreference == Models.RefundPreference.AccountCredit.ToString())
            {
                _context.CustomerAccountMovements.Add(new CustomerAccountMovement
                {
                    CustomerId = customerId!.Value,
                    MovementType = MovementType.ReturnCredit.ToString(),
                    ReferenceType = "SaleReturn",
                    ReferenceId = saleReturn.Id,
                    Amount = -Math.Abs(refundTotal),
                    Description = $"Return credit from sale {sale.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                _context.CashSessionMoneyMovements.Add(new CashSessionMoneyMovement
                {
                    CashSessionId = cashSessionId,
                    StoreId = sale.StoreId,
                    Method = PaymentMethod.Cash.ToString(),
                    SignedAmount = -Math.Abs(refundTotal),
                    Type = CashSessionMovementType.Refund.ToString(),
                    RefType = "SaleReturn",
                    RefId = saleReturn.Id,
                    CreatedByOperatorSessionId = operatorSessionId,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return await _context.SaleReturns
                .Include(r => r.Lines)
                .FirstAsync(r => r.Id == saleReturn.Id);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<SaleReturn>> GetReturnsAsync(int? saleId = null)
    {
        var query = _context.SaleReturns
            .Include(r => r.Lines)
            .AsQueryable();

        if (saleId.HasValue)
            query = query.Where(r => r.OriginalSaleId == saleId.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<SaleReturn?> GetReturnByIdAsync(int returnId)
    {
        return await _context.SaleReturns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == returnId);
    }

    public async Task<Sale> ImportManualSaleAsync(int cashSessionId, int operatorSessionId, int usuarioId, string externalTicketId, DateTime originalCreatedAt, string? customerAlias, List<(string code, decimal quantity, decimal? unitPrice)> items)
    {
        if (string.IsNullOrWhiteSpace(externalTicketId))
            throw new InvalidOperationException("ExternalTicketId is required");

        var existing = await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.ExternalTicketId == externalTicketId);
        if (existing != null)
            return existing;

        if (items.Count == 0)
            throw new InvalidOperationException("At least one item is required");

        var cashSession = await _context.CashSessions.FindAsync(cashSessionId);
        if (cashSession == null || cashSession.Status != CashSessionStatus.Open.ToString())
            throw new InvalidOperationException("La sesión de caja debe estar abierta");

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var sale = new Sale
            {
                CartId = null,
                DeviceId = cashSession.DeviceId,
                StoreId = cashSession.StoreId,
                OperatorSessionId = operatorSessionId,
                CashSessionId = cashSessionId,
                Status = SaleStatus.Completed.ToString(),
                Subtotal = 0,
                Discount = 0,
                Tax = 0,
                Total = 0,
                InvoiceNumber = await GenerateInvoiceNumber(),
                ExternalTicketId = externalTicketId,
                CreatedOffline = true,
                OfflineSource = "ManualTalonario",
                CreatedAt = originalCreatedAt,
                CompletedAt = originalCreatedAt
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            decimal subtotal = 0;
            decimal cigaretteSurcharge = 0;

            var surchargePercentRaw = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "CigaretteSurchargePercent");
            var surchargeMethodsRaw = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "CigaretteSurchargeMethods");
            var surchargePercent = surchargePercentRaw != null && decimal.TryParse(surchargePercentRaw.Value, out var p) ? p : 0;
            var surchargeMethods = surchargeMethodsRaw?.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>();

            foreach (var line in items)
            {
                if (line.quantity <= 0)
                    throw new InvalidOperationException("Quantity must be > 0");

                var product = await _productLookupService.ResolveByScannedCodeAsync(line.code);
                if (product == null)
                    throw new InvalidOperationException($"Product not found for code: {line.code}");

                var generalPrice = await _context.ProductPrices
                    .Include(pp => pp.PriceList)
                    .Where(pp => pp.ProductId == product.Id && pp.PriceList.IsDefault)
                    .OrderByDescending(pp => pp.CreatedAt)
                    .FirstOrDefaultAsync();

                var unitPrice = line.unitPrice ?? (product.SaleType == SaleType.Weight.ToString()
                    ? (generalPrice?.PricePerKg ?? product.DefaultPricePerKg)
                    : (generalPrice?.Price ?? product.DefaultPrice));

                var unit = product.SaleType == SaleType.Weight.ToString() ? MeasureUnit.Weight.ToString() : MeasureUnit.Unit.ToString();
                var lineSubtotal = Math.Round(unitPrice * line.quantity, 2, MidpointRounding.AwayFromZero);

                _context.SaleItems.Add(new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = product.Id,
                    ProductCode = line.code,
                    ProductName = product.Name,
                    UnitPrice = unitPrice,
                    Quantity = line.quantity,
                    Unit = unit,
                    Discount = 0,
                    CigaretteSurcharge = 0
                });

                if (product.StockControl)
                {
                    await _stockService.ApplyMovementAsync(
                        product.Id,
                        StockBucket.VENDIBLE.ToString(),
                        -line.quantity,
                        StockMovementType.Sale.ToString(),
                        saleId: sale.Id,
                        operatorSessionId: operatorSessionId,
                        deviceId: sale.DeviceId,
                        notes: $"Offline manual sale ticket {externalTicketId}"
                    );
                }

                if (product.IsCigarette && surchargePercent > 0 && surchargeMethods.Contains(PaymentMethod.Cash.ToString()))
                {
                    cigaretteSurcharge += Math.Round(lineSubtotal * (surchargePercent / 100m), 2, MidpointRounding.AwayFromZero);
                }

                subtotal += lineSubtotal;
            }

            sale.Subtotal = subtotal;
            sale.CigaretteSurcharge = cigaretteSurcharge;
            sale.Total = subtotal + cigaretteSurcharge;

            _context.SalePayments.Add(new SalePayment
            {
                SaleId = sale.Id,
                PaymentMethod = PaymentMethod.Cash.ToString(),
                Status = PaymentStatus.Confirmed.ToString(),
                Amount = sale.Total,
                Reference = externalTicketId,
                ConfirmNotes = customerAlias,
                CreatedAt = originalCreatedAt
            });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return await _context.Sales
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .FirstAsync(s => s.Id == sale.Id);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<string> GenerateInvoiceNumber()
    {
        var year = DateTime.UtcNow.Year;
        var lastSale = await _context.Sales
            .Where(s => s.InvoiceNumber != null && s.InvoiceNumber.StartsWith($"INV-{year}"))
            .OrderByDescending(s => s.InvoiceNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastSale != null && lastSale.InvoiceNumber != null)
        {
            var parts = lastSale.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSeq))
                sequence = lastSeq + 1;
        }

        return $"INV-{year}-{sequence:D5}";
    }
}
