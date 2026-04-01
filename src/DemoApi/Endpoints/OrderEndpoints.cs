using System.Diagnostics;
using DemoApi.Data;
using DemoApi.Metrics;
using DemoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoApi.Endpoints;

public record CreateOrderRequest(string CustomerName, int ProductId, int Quantity);

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders");

        group.MapPost("/", async (CreateOrderRequest request, AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var product = await db.Products.FindAsync(request.ProductId);
                if (product is null)
                {
                    AppMetrics.OrdersCreatedTotal.WithLabels("failed").Inc();
                    return Results.BadRequest(new { error = "Product not found" });
                }

                if (product.Stock < request.Quantity)
                {
                    AppMetrics.OrdersCreatedTotal.WithLabels("failed").Inc();
                    return Results.BadRequest(new { error = "Insufficient stock" });
                }

                product.Stock -= request.Quantity;

                var order = new Order
                {
                    CustomerName = request.CustomerName,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    Total = product.Price * request.Quantity,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                AppMetrics.DbQueryDuration.WithLabels("insert").Observe(sw.Elapsed.TotalSeconds);
                AppMetrics.OrdersCreatedTotal.WithLabels("success").Inc();
                AppMetrics.OrderTotalAmount.Observe((double)order.Total);

                return Results.Created($"/api/orders/{order.Id}", order);
            }
            catch (Exception)
            {
                AppMetrics.DbErrorsTotal.Inc();
                throw;
            }
        });

        group.MapGet("/", async (int? page, int? pageSize, AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var p = page ?? 1;
                var ps = pageSize ?? 10;
                var orders = await db.Orders
                    .AsNoTracking()
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((p - 1) * ps)
                    .Take(ps)
                    .ToListAsync();

                AppMetrics.DbQueryDuration.WithLabels("select").Observe(sw.Elapsed.TotalSeconds);
                return Results.Ok(orders);
            }
            catch (Exception)
            {
                AppMetrics.DbErrorsTotal.Inc();
                throw;
            }
        });

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var order = await db.Orders.FindAsync(id);
                AppMetrics.DbQueryDuration.WithLabels("select").Observe(sw.Elapsed.TotalSeconds);
                return order is null ? Results.NotFound() : Results.Ok(order);
            }
            catch (Exception)
            {
                AppMetrics.DbErrorsTotal.Inc();
                throw;
            }
        });

        group.MapPost("/{id:int}/cancel", async (int id, AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var order = await db.Orders.FindAsync(id);
                if (order is null)
                    return Results.NotFound();

                if (order.Status is OrderStatus.Shipped or OrderStatus.Failed or OrderStatus.Cancelled)
                    return Results.BadRequest(new { error = $"Cannot cancel order with status {order.Status}" });

                order.Status = OrderStatus.Cancelled;
                await db.SaveChangesAsync();

                AppMetrics.DbQueryDuration.WithLabels("update").Observe(sw.Elapsed.TotalSeconds);
                return Results.Ok(order);
            }
            catch (Exception)
            {
                AppMetrics.DbErrorsTotal.Inc();
                throw;
            }
        });
    }
}
