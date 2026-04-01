using DemoApi.Data;
using DemoApi.Metrics;
using DemoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoApi.Services;

public class SystemMetricsCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemMetricsCollector> _logger;

    public SystemMetricsCollector(IServiceScopeFactory scopeFactory, ILogger<SystemMetricsCollector> logger)
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
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pendingCount = await db.Orders
                    .CountAsync(o => o.Status == OrderStatus.Pending, stoppingToken);
                AppMetrics.OrdersPendingCount.Set(pendingCount);

                var outOfStockCount = await db.Products
                    .CountAsync(p => p.Stock == 0, stoppingToken);
                AppMetrics.ProductsOutOfStock.Set(outOfStockCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting system metrics");
            }
        }
    }
}
