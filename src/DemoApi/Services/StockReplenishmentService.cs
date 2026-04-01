using DemoApi.Data;
using DemoApi.Metrics;
using Microsoft.EntityFrameworkCore;

namespace DemoApi.Services;

public class StockReplenishmentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockReplenishmentService> _logger;

    public StockReplenishmentService(IServiceScopeFactory scopeFactory, ILogger<StockReplenishmentService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var lowStockProducts = await db.Products
                    .Where(p => p.Stock < 5)
                    .ToListAsync(stoppingToken);

                foreach (var product in lowStockProducts)
                {
                    product.Stock = 50;
                    AppMetrics.StockReplenishedTotal.WithLabels(product.Name).Inc();
                    _logger.LogInformation("Restocked {ProductName} to 50 units", product.Name);
                }

                if (lowStockProducts.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replenishing stock");
            }
        }
    }
}
