using System.Diagnostics;
using DemoApi.Data;
using DemoApi.Metrics;
using Microsoft.EntityFrameworkCore;

namespace DemoApi.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var products = await db.Products.AsNoTracking().ToListAsync();
                AppMetrics.DbQueryDuration.WithLabels("select").Observe(sw.Elapsed.TotalSeconds);
                return Results.Ok(products);
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
                var product = await db.Products.FindAsync(id);
                AppMetrics.DbQueryDuration.WithLabels("select").Observe(sw.Elapsed.TotalSeconds);
                return product is null ? Results.NotFound() : Results.Ok(product);
            }
            catch (Exception)
            {
                AppMetrics.DbErrorsTotal.Inc();
                throw;
            }
        });
    }
}
