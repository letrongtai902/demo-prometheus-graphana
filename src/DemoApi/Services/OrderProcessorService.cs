using System.Diagnostics;
using DemoApi.Data;
using DemoApi.Metrics;
using DemoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoApi.Services;

public class OrderProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderProcessorService> _logger;
    private static readonly Random Random = new();

    public OrderProcessorService(IServiceScopeFactory scopeFactory, ILogger<OrderProcessorService> logger)
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
                await Task.Delay(TimeSpan.FromSeconds(Random.Next(3, 6)), stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pendingOrders = await db.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .ToListAsync(stoppingToken);

                AppMetrics.OrdersPendingCount.Set(pendingOrders.Count);

                foreach (var order in pendingOrders)
                {
                    var sw = Stopwatch.StartNew();

                    order.Status = OrderStatus.Processing;
                    await db.SaveChangesAsync(stoppingToken);

                    await Task.Delay(TimeSpan.FromMilliseconds(Random.Next(100, 2000)), stoppingToken);

                    if (Random.NextDouble() < 0.9)
                    {
                        order.Status = OrderStatus.Shipped;
                        AppMetrics.OrdersProcessedTotal.WithLabels("shipped").Inc();
                        _logger.LogInformation("Order {OrderId} shipped successfully", order.Id);
                    }
                    else
                    {
                        order.Status = OrderStatus.Failed;
                        AppMetrics.OrdersProcessedTotal.WithLabels("failed").Inc();
                        _logger.LogWarning("Order {OrderId} processing failed", order.Id);
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    sw.Stop();
                    AppMetrics.OrderProcessingDuration.Observe(sw.Elapsed.TotalSeconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orders");
            }
        }
    }
}
