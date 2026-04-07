using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public class LateFeeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LateFeeBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDailyLateFeeAsync(stoppingToken);
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunDailyLateFeeAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateTime.UtcNow.Date;

        var enabledSetting = await context.Settings.FirstOrDefaultAsync(s => s.Key == "LateFeeEnabled", cancellationToken);
        var enabled = enabledSetting == null || string.Equals(enabledSetting.Value, "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
            return;

        decimal lateFeePercentage = 5m;
        var percentSetting = await context.Settings.FirstOrDefaultAsync(s => s.Key == "LateFeePercentMonthly", cancellationToken);
        if (percentSetting != null && decimal.TryParse(percentSetting.Value, out var configured))
            lateFeePercentage = configured;

        var overdueStatements = await context.CustomerMonthlyStatements
            .Where(s => !s.IsPaid && s.DueDate.Date < today && s.LateFeeAppliedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var statement in overdueStatements)
        {
            if (statement.LateFeeAppliedAt != null)
                continue;

            var allocated = await context.CustomerStatementAllocations
                .Where(a => a.StatementId == statement.Id)
                .SumAsync(a => a.Amount, cancellationToken);

            var remainingReal = statement.TotalAmount - allocated;
            if (remainingReal <= 0)
                continue;

            var lateFeeAmount = Math.Round(remainingReal * (lateFeePercentage / 100m), 2, MidpointRounding.AwayFromZero);
            if (lateFeeAmount <= 0)
                continue;

            context.CustomerAccountMovements.Add(new CustomerAccountMovement
            {
                CustomerId = statement.CustomerId,
                MovementType = MovementType.LateFee.ToString(),
                ReferenceType = "MonthlyStatement",
                ReferenceId = statement.Id,
                Amount = lateFeeAmount,
                Description = $"Automatic late fee {today:yyyy-MM-dd}",
                CreatedAt = DateTime.UtcNow
            });

            statement.LateFees += lateFeeAmount;
            statement.LateFeeAccrued += lateFeeAmount;
            statement.FinalBalance += lateFeeAmount;
            statement.TotalAmount += lateFeeAmount;
            statement.RemainingBalance += lateFeeAmount;
            statement.LateFeeAppliedAt = DateTime.UtcNow;
            statement.LateFeeAppliedAmount = lateFeeAmount;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
