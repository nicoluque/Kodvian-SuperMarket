using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/customers")]
[OperatorSessionAuth]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ICashSessionService _cashSessionService;

    public CustomersController(ICustomerService customerService, ICashSessionService cashSessionService)
    {
        _customerService = customerService;
        _cashSessionService = cashSessionService;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CustomerCreateRequest request)
    {
        try
        {
            var customer = await _customerService.CreateAsync(
                request.FullName,
                request.DNI,
                request.Address,
                request.Phone,
                request.PhoneBackup,
                request.BirthDate,
                request.IsFixedCustomer,
                request.AllowsCredit,
                request.CreditLimit,
                request.Status
            );

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, await ToResponseAsync(customer));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> GetAll([FromQuery] bool? active = null)
    {
        var customers = await _customerService.GetAllAsync(active);
        var mapped = new List<CustomerResponse>();
        foreach (var customer in customers)
            mapped.Add(await ToResponseAsync(customer));
        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound();
        return Ok(await ToResponseAsync(customer));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, [FromBody] CustomerUpdateRequest request)
    {
        try
        {
            var customer = await _customerService.UpdateAsync(
                id,
                request.FullName,
                request.DNI,
                request.Address,
                request.Phone,
                request.PhoneBackup,
                request.BirthDate,
                request.IsFixedCustomer,
                request.AllowsCredit,
                request.CreditLimit,
                request.IsActive,
                request.Status
            );
            return Ok(await ToResponseAsync(customer));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<CustomerResponse>> UpdateStatus(int id, [FromBody] CustomerStatusUpdateRequest request)
    {
        try
        {
            var customer = await _customerService.SetStatusAsync(id, request.Status, request.Reason);
            return Ok(await ToResponseAsync(customer));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<CustomerResponse>> Deactivate(int id, [FromBody] CustomerStatusReasonRequest? request = null)
    {
        try
        {
            var customer = await _customerService.SetStatusAsync(id, CustomerStatus.Inactive.ToString(), request?.Reason);
            return Ok(await ToResponseAsync(customer));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/reactivate")]
    public async Task<ActionResult<CustomerResponse>> Reactivate(int id)
    {
        try
        {
            var customer = await _customerService.SetStatusAsync(id, CustomerStatus.Active.ToString());
            return Ok(await ToResponseAsync(customer));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/account-summary")]
    public async Task<ActionResult<CustomerAccountSummaryResponse>> GetAccountSummary(int id)
    {
        try
        {
            var summary = await _customerService.GetAccountSummaryAsync(id);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/movements")]
    public async Task<ActionResult<CustomerAccountMovementsResponse>> GetMovements(int id)
    {
        try
        {
            var movements = await _customerService.GetAccountMovementsAsync(id);
            return Ok(new CustomerAccountMovementsResponse
            {
                CustomerId = id,
                Movements = movements
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("payment")]
    public async Task<ActionResult<CustomerPaymentResponse>> ProcessPayment([FromBody] CustomerPaymentRequest request)
    {
        try
        {
            var result = await _customerService.ProcessPaymentAsync(
                request.CustomerId,
                request.Amount,
                request.Reference,
                request.Notes
            );
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("statements/generate")]
    public async Task<ActionResult> GenerateStatements([FromBody] GenerateStatementRequest request)
    {
        await _customerService.GenerateMonthlyStatementAsync(request.Year, request.Month, request.CustomerId);
        return Ok(new { message = "Statements generated successfully" });
    }

    [HttpPost("statements/run-late-fee")]
    public async Task<ActionResult> RunLateFee([FromBody] RunLateFeeRequest request)
    {
        await _customerService.RunLateFeeAsync(request.Year, request.Month, request.LateFeePercentage);
        return Ok(new { message = "Late fees applied successfully" });
    }

    [HttpGet("{id}/statements")]
    public async Task<ActionResult<List<StatementResponse>>> GetStatements(int id)
    {
        var statements = await _customerService.GetStatementsAsync(id);
        return Ok(statements.Select(ToStatementResponse).ToList());
    }

    [HttpGet("{id}/can-fiado")]
    public async Task<ActionResult<object>> CanCreateFiado(int id)
    {
        var canFiado = await _customerService.CanCreateFiadoAsync(id);
        return Ok(new { canFiado });
    }

    [HttpPost("anonymous")]
    public async Task<ActionResult<CustomerResponse>> CreateAnonymous([FromBody] AnonymousCustomerCreateRequest request)
    {
        var customer = await _customerService.CreateAnonymousAsync(request.Alias);
        return Ok(await ToResponseAsync(customer));
    }

    [HttpGet("{id}/containers/summary")]
    public async Task<ActionResult<CustomerContainerSummaryResponse>> GetContainerSummary(int id)
    {
        try
        {
            var summary = await _customerService.GetContainerSummaryAsync(id);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/containers/return")]
    [DeviceAuth]
    public async Task<ActionResult> RegisterContainerReturn(int id, [FromBody] CustomerContainerReturnRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden registrar devoluciones de envases" });

        var cashSession = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
        if (cashSession == null)
            return BadRequest(new { message = "No hay una caja abierta" });

        try
        {
            await _customerService.RegisterContainerReturnAsync(id, cashSession.Id, operatorSessionId, usuarioId, request.ContainerTypeId, request.Qty);
            await _cashSessionService.RecalculateTotalsAsync(cashSession.Id);
            return Ok(new { message = "Devolución de envases registrada" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<CustomerResponse> ToResponseAsync(Customer customer)
    {
        var op = await _customerService.GetOperationalStatusAsync(customer);

        var baseStatus = string.IsNullOrWhiteSpace(customer.Status)
            ? (customer.IsActive ? CustomerStatus.Active.ToString() : CustomerStatus.Inactive.ToString())
            : customer.Status;

        return new CustomerResponse
        {
            Id = customer.Id,
            FullName = customer.FullName,
            DNI = customer.DNI,
            Address = customer.Address,
            Phone = customer.Phone,
            PhoneBackup = customer.PhoneBackup,
            BirthDate = customer.BirthDate,
            IsFixedCustomer = customer.IsFixedCustomer,
            AllowsCredit = customer.AllowsCredit,
            CreditLimit = customer.CreditLimit,
            IsActive = customer.IsActive,
            IsAnonymous = customer.IsAnonymous,
            CreatedAt = customer.CreatedAt,
            Status = baseStatus,
            EffectiveStatus = op.EffectiveStatus,
            CurrentDebt = op.CurrentDebt,
            AvailableCredit = op.AvailableCredit,
            CreditUsedPct = op.CreditUsedPct,
            IsCritical = op.IsCritical,
            IsCreditBlocked = op.IsCreditBlocked
        };
    }

    private static StatementResponse ToStatementResponse(CustomerMonthlyStatement statement)
    {
        return new StatementResponse
        {
            Id = statement.Id,
            CustomerId = statement.CustomerId,
            CustomerName = "",
            Year = statement.Year,
            Month = statement.Month,
            InitialBalance = statement.InitialBalance,
            Purchases = statement.Purchases,
            Payments = statement.Payments,
            LateFees = statement.LateFees,
            FinalBalance = statement.FinalBalance,
            PaidAmount = statement.PaidAmount,
            RemainingBalance = statement.RemainingBalance,
            DueDate = statement.DueDate,
            IsPaid = statement.IsPaid,
            PaidAt = statement.PaidAt
        };
    }
}
