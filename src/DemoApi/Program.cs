using DemoApi.Data;
using DemoApi.Endpoints;
using DemoApi.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=demo.db"));

builder.Services.AddHostedService<OrderProcessorService>();
builder.Services.AddHostedService<StockReplenishmentService>();
builder.Services.AddHostedService<SystemMetricsCollector>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

app.UseHttpMetrics();

// Catch unhandled exceptions and set the status code to 500
// so prometheus-net records the correct code (not 200).
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal Server Error");
        }
    }
});

app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapGet("/api/simulate-error", () =>
{
    throw new InvalidOperationException("Simulated server error for alerting demo");
});

app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
