using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.DTOs;

namespace KodvianSuperMarket.Services;

public interface ICustomerService
{
    Task<Customer> CreateAsync(string fullName, string? dni, string? address, string? phone, string? phoneBackup, DateTime? birthDate, bool isFixedCustomer, bool allowsCredit, decimal creditLimit, string? status);
    Task<Customer?> GetByIdAsync(int id);
    Task<List<Customer>> GetAllAsync(bool? active = null);
    Task<Customer> UpdateAsync(int id, string? fullName, string? dni, string? address, string? phone, string? phoneBackup, DateTime? birthDate, bool? isFixedCustomer, bool? allowsCredit, decimal? creditLimit, bool? isActive, string? status);
    Task<Customer> SetStatusAsync(int id, string status, string? reason = null);
    Task<CustomerAccountSummaryResponse> GetAccountSummaryAsync(int customerId);
    Task<(decimal CurrentDebt, decimal AvailableCredit, decimal CreditUsedPct, bool IsCritical, bool IsCreditBlocked, string EffectiveStatus)> GetOperationalStatusAsync(Customer customer);
    Task<Dictionary<int, (decimal CurrentDebt, decimal AvailableCredit, decimal CreditUsedPct, bool IsCritical, bool IsCreditBlocked, string EffectiveStatus)>> GetOperationalStatusesAsync(IEnumerable<Customer> customers);
    Task<List<MovementResponse>> GetAccountMovementsAsync(int customerId);
    Task<CustomerPaymentResponse> ProcessPaymentAsync(int customerId, decimal amount, string? reference, string? notes);
    Task GenerateMonthlyStatementAsync(int year, int month, int? customerId = null);
    Task RunLateFeeAsync(int year, int month, decimal lateFeePercentage);
    Task<List<CustomerMonthlyStatement>> GetStatementsAsync(int customerId);
    Task<bool> CanCreateFiadoAsync(int customerId, decimal amount = 0m);
    Task<Sale> ProcessAccountCreditPaymentAsync(Sale sale, int customerId, decimal amount);
    Task<Customer> CreateAnonymousAsync(string alias);
    Task<CustomerContainerSummaryResponse> GetContainerSummaryAsync(int customerId);
    Task RegisterContainerReturnAsync(int customerId, int cashSessionId, int operatorSessionId, int usuarioId, int containerTypeId, decimal qty);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private const decimal CriticalThresholdPct = 90m;
    private const decimal BlockThresholdPct = 100m;
    private const decimal CreditToleranceFactor = 1.10m;

    public CustomerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Customer> CreateAsync(string fullName, string? dni, string? address, string? phone, string? phoneBackup, DateTime? birthDate, bool isFixedCustomer, bool allowsCredit, decimal creditLimit, string? status)
    {
        var normalizedStatus = NormalizeStatus(status, CustomerStatus.Active);
        var customer = new Customer
        {
            FullName = fullName,
            DNI = dni,
            Address = address,
            Phone = phone,
            PhoneBackup = phoneBackup,
            BirthDate = birthDate,
            IsFixedCustomer = isFixedCustomer,
            AllowsCredit = allowsCredit,
            CreditLimit = creditLimit,
            Status = normalizedStatus,
            IsActive = normalizedStatus == CustomerStatus.Active.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        if (customer.Status == CustomerStatus.Inactive.ToString())
            customer.AllowsCredit = false;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return customer;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<List<Customer>> GetAllAsync(bool? active = null)
    {
        var query = _context.Customers.AsQueryable();
        if (active.HasValue)
            query = query.Where(c => c.IsActive == active.Value);
        return await query.OrderBy(c => c.FullName).ToListAsync();
    }

    public async Task<Customer> UpdateAsync(int id, string? fullName, string? dni, string? address, string? phone, string? phoneBackup, DateTime? birthDate, bool? isFixedCustomer, bool? allowsCredit, decimal? creditLimit, bool? isActive, string? status)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            throw new InvalidOperationException("Cliente no encontrado");

        if (fullName != null) customer.FullName = fullName;
        if (dni != null) customer.DNI = dni;
        if (address != null) customer.Address = address;
        if (phone != null) customer.Phone = phone;
        if (phoneBackup != null) customer.PhoneBackup = phoneBackup;
        if (birthDate.HasValue) customer.BirthDate = birthDate;
        if (isFixedCustomer.HasValue) customer.IsFixedCustomer = isFixedCustomer.Value;
        if (allowsCredit.HasValue) customer.AllowsCredit = allowsCredit.Value;
        if (creditLimit.HasValue) customer.CreditLimit = creditLimit.Value;

        if (!string.IsNullOrWhiteSpace(status))
        {
            customer.Status = NormalizeStatus(status, customer.IsActive ? CustomerStatus.Active : CustomerStatus.Inactive);
            customer.IsActive = customer.Status == CustomerStatus.Active.ToString();
        }
        else if (isActive.HasValue)
        {
            customer.IsActive = isActive.Value;
            customer.Status = customer.IsActive ? CustomerStatus.Active.ToString() : CustomerStatus.Inactive.ToString();
        }

        if (customer.Status == CustomerStatus.Inactive.ToString())
            customer.AllowsCredit = false;

        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> SetStatusAsync(int id, string status, string? reason = null)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            throw new InvalidOperationException("Cliente no encontrado");

        var normalized = NormalizeStatus(status, customer.IsActive ? CustomerStatus.Active : CustomerStatus.Inactive);
        customer.Status = normalized;
        customer.IsActive = normalized == CustomerStatus.Active.ToString();

        if (normalized == CustomerStatus.Inactive.ToString())
            customer.AllowsCredit = false;

        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<CustomerAccountSummaryResponse> GetAccountSummaryAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("Cliente no encontrado");

        var movements = await _context.CustomerAccountMovements
            .Where(m => m.CustomerId == customerId)
            .ToListAsync();

        var totalDebt = movements.Where(m => m.MovementType == MovementType.CreditPurchase.ToString() || m.MovementType == MovementType.LateFee.ToString())
            .Sum(m => m.Amount);
        var totalCredit = movements.Where(m => m.MovementType == MovementType.Payment.ToString() || m.MovementType == MovementType.CreditNote.ToString())
            .Sum(m => Math.Abs(m.Amount));
        var allocatedPayments = movements.Where(m => m.MovementType == MovementType.Payment.ToString() && m.AllocatedStatementId != null)
            .Sum(m => Math.Abs(m.Amount));

        var totalPaid = allocatedPayments;
        var netDebt = totalDebt - totalPaid;

        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        var overdueStatement = await _context.CustomerMonthlyStatements
            .Where(s => s.CustomerId == customerId && !s.IsPaid && s.DueDate < DateTime.UtcNow && (s.Year < currentYear || (s.Year == currentYear && s.Month < currentMonth)))
            .FirstOrDefaultAsync();

        return new CustomerAccountSummaryResponse
        {
            CustomerId = customerId,
            CustomerName = customer.FullName,
            TotalDebt = netDebt > 0 ? netDebt : 0,
            TotalCredit = netDebt < 0 ? Math.Abs(netDebt) : 0,
            AvailableCredit = Math.Max(0, customer.CreditLimit - (netDebt > 0 ? netDebt : 0)),
            CreditLimit = customer.CreditLimit,
            HasOverdueDebt = overdueStatement != null
        };
    }

    public async Task<List<MovementResponse>> GetAccountMovementsAsync(int customerId)
    {
        var movements = await _context.CustomerAccountMovements
            .Where(m => m.CustomerId == customerId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return movements.Select(m => new MovementResponse
        {
            Id = m.Id,
            MovementType = m.MovementType,
            ReferenceType = m.ReferenceType,
            ReferenceId = m.ReferenceId,
            Amount = m.Amount,
            Description = m.Description,
            CreatedAt = m.CreatedAt,
            IsAllocated = m.AllocatedStatementId != null
        }).ToList();
    }

    public async Task<CustomerPaymentResponse> ProcessPaymentAsync(int customerId, decimal amount, string? reference, string? notes)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("Customer not found");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var paymentMovement = new CustomerAccountMovement
            {
                CustomerId = customerId,
                MovementType = MovementType.Payment.ToString(),
                ReferenceType = "Payment",
                Amount = -Math.Abs(amount),
                Description = $"Payment: {notes ?? "Payment processed"}",
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerAccountMovements.Add(paymentMovement);
            await _context.SaveChangesAsync();

            var allocations = new List<AllocationResponse>();

            var pendingStatements = await _context.CustomerMonthlyStatements
                .Where(s => s.CustomerId == customerId && !s.IsPaid && s.RemainingBalance > 0)
                .OrderBy(s => s.Year).ThenBy(s => s.Month)
                .ToListAsync();

            var remainingAmount = amount;

            foreach (var statement in pendingStatements)
            {
                if (remainingAmount <= 0) break;

                var allocationAmount = Math.Min(remainingAmount, statement.RemainingBalance);

                var allocation = new CustomerStatementAllocation
                {
                    StatementId = statement.Id,
                    MovementId = paymentMovement.Id,
                    Amount = allocationAmount,
                    AllocatedAt = DateTime.UtcNow
                };
                _context.CustomerStatementAllocations.Add(allocation);

                var prevBalance = statement.RemainingBalance;
                statement.PaidAmount += allocationAmount;
                statement.RemainingBalance -= allocationAmount;

                if (statement.RemainingBalance <= 0.01m)
                {
                    statement.IsPaid = true;
                    statement.PaidAt = DateTime.UtcNow;
                }

                allocations.Add(new AllocationResponse
                {
                    StatementId = statement.Id,
                    Year = statement.Year,
                    Month = statement.Month,
                    Amount = allocationAmount,
                    PreviousBalance = prevBalance,
                    NewBalance = statement.RemainingBalance
                });

                remainingAmount -= allocationAmount;
            }

            paymentMovement.AllocatedStatementId = allocations.FirstOrDefault()?.StatementId;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var summary = await GetAccountSummaryAsync(customerId);

            return new CustomerPaymentResponse
            {
                Id = paymentMovement.Id,
                CustomerId = customerId,
                Amount = amount,
                Reference = reference,
                Allocations = allocations,
                RemainingCredit = summary.TotalCredit,
                CreatedAt = paymentMovement.CreatedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task GenerateMonthlyStatementAsync(int year, int month, int? customerId = null)
    {
        List<Customer> customers;
        if (customerId.HasValue)
        {
            var singleCustomer = await _context.Customers.FindAsync(customerId.Value);
            if (singleCustomer == null)
                throw new InvalidOperationException("Cliente no encontrado");

            customers = new List<Customer> { singleCustomer };
        }
        else
        {
            customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        }

        foreach (var customer in customers)
        {
            var existingStatement = await _context.CustomerMonthlyStatements
                .FirstOrDefaultAsync(s => s.CustomerId == customer.Id && s.Year == year && s.Month == month);

            if (existingStatement != null) continue;

            var previousMonth = month == 1 ? 12 : month - 1;
            var previousYear = month == 1 ? year - 1 : year;

            var previousStatement = await _context.CustomerMonthlyStatements
                .FirstOrDefaultAsync(s => s.CustomerId == customer.Id && s.Year == previousYear && s.Month == previousMonth);

            var initialBalance = previousStatement?.RemainingBalance ?? 0;

            var purchases = await _context.CustomerAccountMovements
                .Where(m => m.CustomerId == customer.Id && 
                           m.MovementType == MovementType.CreditPurchase.ToString() &&
                           m.CreatedAt.Year == year && 
                           m.CreatedAt.Month == month)
                .SumAsync(m => m.Amount);

            var payments = await _context.CustomerAccountMovements
                .Where(m => m.CustomerId == customer.Id && 
                           m.MovementType == MovementType.Payment.ToString() &&
                           m.CreatedAt.Year == year && 
                           m.CreatedAt.Month == month)
                .SumAsync(m => Math.Abs(m.Amount));

            var lateFees = await _context.CustomerAccountMovements
                .Where(m => m.CustomerId == customer.Id && 
                           m.MovementType == MovementType.LateFee.ToString() &&
                           m.CreatedAt.Year == year && 
                           m.CreatedAt.Month == month)
                .SumAsync(m => m.Amount);

            var finalBalance = initialBalance + purchases + lateFees - payments;
            var dueDate = new DateTime(year, month, 1).AddMonths(1).AddDays(9);

            var statement = new CustomerMonthlyStatement
            {
                CustomerId = customer.Id,
                Year = year,
                Month = month,
                InitialBalance = initialBalance,
                Purchases = purchases,
                Payments = payments,
                LateFees = lateFees,
                FinalBalance = finalBalance,
                TotalAmount = finalBalance,
                LateFeeAccrued = lateFees,
                PaidAmount = 0,
                RemainingBalance = finalBalance,
                DueDate = dueDate,
                IsPaid = false,
                LateFeeAppliedAt = null,
                LateFeeAppliedAmount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerMonthlyStatements.Add(statement);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RunLateFeeAsync(int year, int month, decimal lateFeePercentage)
    {
        var statements = await _context.CustomerMonthlyStatements
            .Where(s => s.Year == year && s.Month == month && !s.IsPaid && s.RemainingBalance > 0)
            .ToListAsync();

        var dueDate = new DateTime(year, month, 1).AddMonths(1).AddDays(9);

        foreach (var statement in statements)
        {
            if (DateTime.UtcNow < dueDate) continue;

            var lateFeeAmount = statement.RemainingBalance * (lateFeePercentage / 100);

            var lateFeeMovement = new CustomerAccountMovement
            {
                CustomerId = statement.CustomerId,
                MovementType = MovementType.LateFee.ToString(),
                ReferenceType = "MonthlyStatement",
                ReferenceId = statement.Id,
                Amount = lateFeeAmount,
                Description = $"Late fee for {year}/{month:D2}",
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerAccountMovements.Add(lateFeeMovement);

            statement.LateFees += lateFeeAmount;
            statement.LateFeeAccrued += lateFeeAmount;
            statement.FinalBalance += lateFeeAmount;
            statement.TotalAmount += lateFeeAmount;
            statement.RemainingBalance += lateFeeAmount;
            statement.LateFeeAppliedAt = DateTime.UtcNow;
            statement.LateFeeAppliedAmount = lateFeeAmount;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<CustomerMonthlyStatement>> GetStatementsAsync(int customerId)
    {
        return await _context.CustomerMonthlyStatements
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
            .ToListAsync();
    }

    public async Task<bool> CanCreateFiadoAsync(int customerId, decimal amount = 0m)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null || !customer.IsActive)
            return false;

        var baseStatus = ResolveBaseStatus(customer);
        if (baseStatus != CustomerStatus.Active.ToString())
            return false;

        if (!customer.AllowsCredit)
            return false;

        var summary = await GetAccountSummaryAsync(customerId);
        if (summary.HasOverdueDebt)
            return false;

        var operational = await GetOperationalStatusAsync(customer);
        if (operational.IsCreditBlocked)
            return false;

        var projectedAmount = Math.Max(0m, amount);
        var projectedDebt = operational.CurrentDebt + projectedAmount;
        var toleranceLimit = customer.CreditLimit * CreditToleranceFactor;
        if (customer.CreditLimit <= 0 || projectedDebt > toleranceLimit)
            return false;

        return true;
    }

    public async Task<Sale> ProcessAccountCreditPaymentAsync(Sale sale, int customerId, decimal amount)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("Cliente no encontrado");

        var operational = await GetOperationalStatusAsync(customer);

        var baseStatus = ResolveBaseStatus(customer);
        if (!customer.AllowsCredit || baseStatus != CustomerStatus.Active.ToString())
            throw new InvalidOperationException("El cliente no tiene cuenta corriente habilitada");

        if (operational.IsCreditBlocked)
            throw new InvalidOperationException("Cliente con cuenta corriente bloqueada por límite de crédito alcanzado");

        var projectedDebt = operational.CurrentDebt + Math.Max(0m, amount);
        var toleranceLimit = customer.CreditLimit * CreditToleranceFactor;
        if (projectedDebt > toleranceLimit)
            throw new InvalidOperationException("No se puede cargar a cuenta corriente: supera el 110% del límite de crédito");

        var paymentMovement = new CustomerAccountMovement
        {
            CustomerId = customerId,
            MovementType = MovementType.CreditPurchase.ToString(),
            ReferenceType = "Sale",
            ReferenceId = sale.Id,
            Amount = amount,
            Description = $"Purchase - Sale #{sale.Id}",
            CreatedAt = DateTime.UtcNow
        };

        _context.CustomerAccountMovements.Add(paymentMovement);
        await _context.SaveChangesAsync();

        return sale;
    }

    public async Task<Customer> CreateAnonymousAsync(string alias)
    {
        var customer = new Customer
        {
            FullName = alias,
            IsAnonymous = true,
            IsActive = true,
            AllowsCredit = true,
            Status = CustomerStatus.Active.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<CustomerContainerSummaryResponse> GetContainerSummaryAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("Customer not found");

        var movements = await _context.ContainerMovements
            .Include(m => m.ContainerType)
            .Where(m => m.CustomerId == customerId)
            .ToListAsync();

        var grouped = movements
            .GroupBy(m => new { m.ContainerTypeId, Name = m.ContainerType.Name })
            .Select(g => new CustomerContainerOwedItem
            {
                ContainerTypeId = g.Key.ContainerTypeId,
                ContainerTypeName = g.Key.Name,
                OwedQty = g.Where(x => x.Direction == ContainerDirection.Given.ToString()).Sum(x => x.Qty)
                    - g.Where(x => x.Direction == ContainerDirection.Returned.ToString()).Sum(x => x.Qty)
            })
            .Where(x => x.OwedQty > 0)
            .OrderBy(x => x.ContainerTypeName)
            .ToList();

        return new CustomerContainerSummaryResponse
        {
            CustomerId = customerId,
            OwedByType = grouped
        };
    }

    public async Task RegisterContainerReturnAsync(int customerId, int cashSessionId, int operatorSessionId, int usuarioId, int containerTypeId, decimal qty)
    {
        if (qty <= 0)
            throw new InvalidOperationException("Qty must be greater than zero");

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("Customer not found");

        var containerType = await _context.ContainerTypes.FindAsync(containerTypeId);
        if (containerType == null)
            throw new InvalidOperationException("Container type not found");

        var summary = await GetContainerSummaryAsync(customerId);
        var owed = summary.OwedByType.FirstOrDefault(x => x.ContainerTypeId == containerTypeId)?.OwedQty ?? 0;
        if (qty > owed)
            throw new InvalidOperationException("Return qty exceeds owed qty");

        _context.ContainerMovements.Add(new ContainerMovement
        {
            CustomerId = customerId,
            ContainerTypeId = containerTypeId,
            Direction = ContainerDirection.Returned.ToString(),
            Qty = qty,
            RefType = "ManualReturn",
            CreatedByOperatorSessionId = operatorSessionId,
            CreatedByUsuarioId = usuarioId,
            CreatedAt = DateTime.UtcNow
        });

        var amount = Math.Round(-(qty * containerType.DepositAmount), 2, MidpointRounding.AwayFromZero);
        _context.CustomerAccountMovements.Add(new CustomerAccountMovement
        {
            CustomerId = customerId,
            MovementType = MovementType.ContainerRefund.ToString(),
            ReferenceType = "ContainerReturn",
            Amount = amount,
            Description = $"Container return {containerType.Name} x{qty}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<(decimal CurrentDebt, decimal AvailableCredit, decimal CreditUsedPct, bool IsCritical, bool IsCreditBlocked, string EffectiveStatus)> GetOperationalStatusAsync(Customer customer)
    {
        var snapshots = await GetOperationalStatusesAsync(new[] { customer });
        var resolved = ResolveBaseStatus(customer);
        return snapshots.TryGetValue(customer.Id, out var status)
            ? status
            : (0m, customer.CreditLimit, 0m, false, true, resolved);
    }

    public async Task<Dictionary<int, (decimal CurrentDebt, decimal AvailableCredit, decimal CreditUsedPct, bool IsCritical, bool IsCreditBlocked, string EffectiveStatus)>> GetOperationalStatusesAsync(IEnumerable<Customer> customers)
    {
        var list = customers.ToList();
        var ids = list.Select(c => c.Id).Distinct().ToList();
        if (ids.Count == 0)
            return new();

        var movements = await _context.CustomerAccountMovements
            .Where(m => ids.Contains(m.CustomerId))
            .Select(m => new { m.CustomerId, m.MovementType, m.Amount, m.AllocatedStatementId })
            .ToListAsync();

        var result = new Dictionary<int, (decimal CurrentDebt, decimal AvailableCredit, decimal CreditUsedPct, bool IsCritical, bool IsCreditBlocked, string EffectiveStatus)>();

        foreach (var customer in list)
        {
            var baseStatus = ResolveBaseStatus(customer);
            var customerMovements = movements.Where(m => m.CustomerId == customer.Id).ToList();

            var totalDebt = customerMovements
                .Where(m => m.MovementType == MovementType.CreditPurchase.ToString() || m.MovementType == MovementType.LateFee.ToString())
                .Sum(m => m.Amount);

            var allocatedPayments = customerMovements
                .Where(m => m.MovementType == MovementType.Payment.ToString() && m.AllocatedStatementId != null)
                .Sum(m => Math.Abs(m.Amount));

            var netDebt = totalDebt - allocatedPayments;
            var currentDebt = netDebt > 0 ? netDebt : 0m;
            var availableCredit = customer.CreditLimit > 0 ? Math.Max(0, customer.CreditLimit - currentDebt) : 0m;
            var creditUsedPct = customer.CreditLimit > 0 ? Math.Round((currentDebt / customer.CreditLimit) * 100m, 2) : 0m;

            var isCritical = customer.CreditLimit > 0
                && baseStatus == CustomerStatus.Active.ToString()
                && customer.AllowsCredit
                && creditUsedPct >= CriticalThresholdPct;

            var isCreditBlocked = baseStatus != CustomerStatus.Active.ToString()
                || !customer.AllowsCredit
                || customer.CreditLimit <= 0
                || creditUsedPct >= BlockThresholdPct;

            var effectiveStatus = isCritical ? "Critical" : baseStatus;

            result[customer.Id] = (currentDebt, availableCredit, creditUsedPct, isCritical, isCreditBlocked, effectiveStatus);
        }

        return result;
    }

    private static string NormalizeStatus(string? status, CustomerStatus fallback)
    {
        var value = status?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return fallback.ToString();

        if (value.Equals("active", StringComparison.OrdinalIgnoreCase)) return CustomerStatus.Active.ToString();
        if (value.Equals("inactive", StringComparison.OrdinalIgnoreCase) || value.Equals("disabled", StringComparison.OrdinalIgnoreCase)) return CustomerStatus.Inactive.ToString();
        if (value.Equals("pending", StringComparison.OrdinalIgnoreCase)) return CustomerStatus.Pending.ToString();

        throw new InvalidOperationException("Estado de cliente inválido");
    }

    private static string ResolveBaseStatus(Customer customer)
    {
        var raw = customer.Status?.Trim();
        if (raw != null)
        {
            if (raw.Equals(CustomerStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase)) return CustomerStatus.Active.ToString();
            if (raw.Equals(CustomerStatus.Inactive.ToString(), StringComparison.OrdinalIgnoreCase)) return CustomerStatus.Inactive.ToString();
            if (raw.Equals(CustomerStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase)) return CustomerStatus.Pending.ToString();
        }

        return customer.IsActive ? CustomerStatus.Active.ToString() : CustomerStatus.Inactive.ToString();
    }
}
