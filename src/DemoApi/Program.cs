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

app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
