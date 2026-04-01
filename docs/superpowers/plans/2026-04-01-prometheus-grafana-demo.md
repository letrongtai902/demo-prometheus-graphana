# Prometheus + Grafana Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Docker Compose-orchestrated demo showcasing Prometheus metrics collection and Grafana visualization using a C# e-commerce API with background services and a traffic simulator.

**Architecture:** ASP.NET Core Web API (DemoApi) with SQLite via EF Core, three background hosted services, and a separate traffic simulator console app. Prometheus scrapes the API's `/metrics` endpoint every 5s. Grafana auto-provisions three dashboards on startup. All four components run as Docker Compose services.

**Tech Stack:** .NET 8, ASP.NET Core Minimal APIs, EF Core + SQLite, prometheus-net, Docker Compose, Prometheus, Grafana

**Parallelization:** Tasks 1-4 are sequential (foundation). After Task 4, Tasks 5-9 can run in parallel. Task 10 depends on 3-9. Task 11 depends only on Task 1. Tasks 12-14 are infrastructure and can run in parallel with all C# tasks (after Task 1 for Dockerfiles). Task 15 depends on all prior tasks.

---

### Task 1: Solution Scaffold

**Files:**
- Create: `DemoPrometheusGrafana.sln`
- Create: `src/DemoApi/DemoApi.csproj`
- Create: `src/DemoApi/Program.cs`
- Create: `src/TrafficSimulator/TrafficSimulator.csproj`
- Create: `src/TrafficSimulator/Program.cs`

- [ ] **Step 1: Create solution and projects**

```bash
cd C:/Users/Admin/Documents/Upwork/Git/demo-prometheus-graphana
dotnet new sln -n DemoPrometheusGrafana
mkdir -p src/DemoApi src/TrafficSimulator
dotnet new web -n DemoApi -o src/DemoApi --no-https
dotnet new console -n TrafficSimulator -o src/TrafficSimulator
dotnet sln add src/DemoApi/DemoApi.csproj
dotnet sln add src/TrafficSimulator/TrafficSimulator.csproj
```

- [ ] **Step 2: Add NuGet packages to DemoApi**

```bash
dotnet add src/DemoApi/DemoApi.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/DemoApi/DemoApi.csproj package prometheus-net.AspNetCore
```

- [ ] **Step 3: Replace DemoApi Program.cs with placeholder**

Write to `src/DemoApi/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "DemoApi running");
app.Run();
```

- [ ] **Step 4: Replace TrafficSimulator Program.cs with placeholder**

Write to `src/TrafficSimulator/Program.cs`:

```csharp
Console.WriteLine("TrafficSimulator placeholder");
```

- [ ] **Step 5: Verify build**

```bash
dotnet build DemoPrometheusGrafana.sln
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add DemoPrometheusGrafana.sln src/DemoApi/ src/TrafficSimulator/
git commit -m "feat: scaffold solution with DemoApi and TrafficSimulator projects"
```

---

### Task 2: Domain Models

**Files:**
- Create: `src/DemoApi/Models/Product.cs`
- Create: `src/DemoApi/Models/Order.cs`

- [ ] **Step 1: Create Product model**

Write to `src/DemoApi/Models/Product.cs`:

```csharp
namespace DemoApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

- [ ] **Step 2: Create Order model with status enum**

Write to `src/DemoApi/Models/Order.cs`:

```csharp
namespace DemoApi.Models;

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Failed,
    Cancelled
}

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/DemoApi/Models/
git commit -m "feat: add Product and Order domain models"
```

---

### Task 3: Data Layer (AppDbContext + SeedData)

**Files:**
- Create: `src/DemoApi/Data/AppDbContext.cs`
- Create: `src/DemoApi/Data/SeedData.cs`

- [ ] **Step 1: Create AppDbContext**

Write to `src/DemoApi/Data/AppDbContext.cs`:

```csharp
using DemoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
}
```

- [ ] **Step 2: Create SeedData**

Write to `src/DemoApi/Data/SeedData.cs`:

```csharp
using DemoApi.Models;

namespace DemoApi.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Products.Any())
            return;

        var products = new Product[]
        {
            new() { Name = "Wireless Mouse", Price = 29.99m, Stock = 50 },
            new() { Name = "Mechanical Keyboard", Price = 89.99m, Stock = 50 },
            new() { Name = "USB-C Hub", Price = 45.00m, Stock = 50 },
            new() { Name = "Monitor Stand", Price = 34.99m, Stock = 50 },
            new() { Name = "Webcam HD", Price = 59.99m, Stock = 50 },
            new() { Name = "Desk Lamp", Price = 24.99m, Stock = 50 },
            new() { Name = "Mouse Pad XL", Price = 19.99m, Stock = 50 },
            new() { Name = "Laptop Stand", Price = 49.99m, Stock = 50 },
            new() { Name = "Cable Management Kit", Price = 14.99m, Stock = 50 },
            new() { Name = "Noise Cancelling Headphones", Price = 99.99m, Stock = 50 },
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/DemoApi/Data/
git commit -m "feat: add AppDbContext and seed data with 10 products"
```

---

### Task 4: Custom Metrics Definitions

**Files:**
- Create: `src/DemoApi/Metrics/AppMetrics.cs`

- [ ] **Step 1: Create AppMetrics with all custom metrics**

Write to `src/DemoApi/Metrics/AppMetrics.cs`:

```csharp
using Prometheus;

namespace DemoApi.Metrics;

public static class AppMetrics
{
    // Business metrics
    public static readonly Counter OrdersCreatedTotal = Prometheus.Metrics.CreateCounter(
        "orders_created_total",
        "Total number of orders created",
        new CounterConfiguration { LabelNames = new[] { "status" } });

    public static readonly Counter OrdersProcessedTotal = Prometheus.Metrics.CreateCounter(
        "orders_processed_total",
        "Total number of orders processed",
        new CounterConfiguration { LabelNames = new[] { "outcome" } });

    public static readonly Histogram OrderProcessingDuration = Prometheus.Metrics.CreateHistogram(
        "order_processing_duration_seconds",
        "Time taken to process an order");

    public static readonly Gauge OrdersPendingCount = Prometheus.Metrics.CreateGauge(
        "orders_pending_count",
        "Number of orders currently pending");

    public static readonly Histogram OrderTotalAmount = Prometheus.Metrics.CreateHistogram(
        "order_total_amount",
        "Distribution of order total amounts",
        new HistogramConfiguration { Buckets = new[] { 10.0, 25.0, 50.0, 100.0, 250.0, 500.0 } });

    public static readonly Counter StockReplenishedTotal = Prometheus.Metrics.CreateCounter(
        "stock_replenished_total",
        "Total stock replenishment events",
        new CounterConfiguration { LabelNames = new[] { "product" } });

    public static readonly Gauge ProductsOutOfStock = Prometheus.Metrics.CreateGauge(
        "products_out_of_stock",
        "Number of products with zero stock");

    // Database metrics
    public static readonly Histogram DbQueryDuration = Prometheus.Metrics.CreateHistogram(
        "db_query_duration_seconds",
        "Database query duration",
        new HistogramConfiguration { LabelNames = new[] { "query_type" } });

    public static readonly Counter DbErrorsTotal = Prometheus.Metrics.CreateCounter(
        "db_errors_total",
        "Total number of database errors");
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Metrics/
git commit -m "feat: add Prometheus custom metric definitions"
```

---

### Task 5: Product Endpoints

**Files:**
- Create: `src/DemoApi/Endpoints/ProductEndpoints.cs`

- [ ] **Step 1: Create ProductEndpoints**

Write to `src/DemoApi/Endpoints/ProductEndpoints.cs`:

```csharp
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
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Endpoints/ProductEndpoints.cs
git commit -m "feat: add product endpoints with DB metrics"
```

---

### Task 6: Order Endpoints

**Files:**
- Create: `src/DemoApi/Endpoints/OrderEndpoints.cs`

- [ ] **Step 1: Create OrderEndpoints**

Write to `src/DemoApi/Endpoints/OrderEndpoints.cs`:

```csharp
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
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Endpoints/OrderEndpoints.cs
git commit -m "feat: add order endpoints with business and DB metrics"
```

---

### Task 7: OrderProcessorService

**Files:**
- Create: `src/DemoApi/Services/OrderProcessorService.cs`

- [ ] **Step 1: Create OrderProcessorService**

Write to `src/DemoApi/Services/OrderProcessorService.cs`:

```csharp
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

                    // Simulate variable processing time
                    await Task.Delay(TimeSpan.FromMilliseconds(Random.Next(100, 2000)), stoppingToken);

                    // 90% success rate
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
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Services/OrderProcessorService.cs
git commit -m "feat: add OrderProcessorService background worker"
```

---

### Task 8: StockReplenishmentService

**Files:**
- Create: `src/DemoApi/Services/StockReplenishmentService.cs`

- [ ] **Step 1: Create StockReplenishmentService**

Write to `src/DemoApi/Services/StockReplenishmentService.cs`:

```csharp
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
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Services/StockReplenishmentService.cs
git commit -m "feat: add StockReplenishmentService background worker"
```

---

### Task 9: SystemMetricsCollector

**Files:**
- Create: `src/DemoApi/Services/SystemMetricsCollector.cs`

This service periodically updates business-level gauges that require DB queries (pending orders count, out-of-stock count). Process-level metrics (CPU, memory, GC) are automatically exported by prometheus-net.

- [ ] **Step 1: Create SystemMetricsCollector**

Write to `src/DemoApi/Services/SystemMetricsCollector.cs`:

```csharp
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
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/DemoApi/DemoApi.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Services/SystemMetricsCollector.cs
git commit -m "feat: add SystemMetricsCollector for gauge updates"
```

---

### Task 10: DemoApi Program.cs — Wire Everything

**Files:**
- Modify: `src/DemoApi/Program.cs`

- [ ] **Step 1: Write the full Program.cs**

Write to `src/DemoApi/Program.cs`:

```csharp
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
```

- [ ] **Step 2: Verify build**

```bash
dotnet build DemoPrometheusGrafana.sln
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Program.cs
git commit -m "feat: wire up DemoApi with EF Core, Prometheus, endpoints, and services"
```

---

### Task 11: Traffic Simulator

**Files:**
- Modify: `src/TrafficSimulator/Program.cs`

- [ ] **Step 1: Write the full Traffic Simulator**

Write to `src/TrafficSimulator/Program.cs`:

```csharp
using System.Net.Http.Json;
using System.Text.Json;

var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
var random = new Random();
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

Console.WriteLine($"Traffic Simulator starting. Target: {baseUrl}");
Console.WriteLine("Waiting for DemoApi to be ready...");

while (true)
{
    try
    {
        var response = await client.GetAsync("/health");
        if (response.IsSuccessStatusCode)
            break;
    }
    catch { }
    Console.WriteLine("  API not ready, retrying in 2s...");
    await Task.Delay(2000);
}

Console.WriteLine("DemoApi is ready! Starting traffic patterns...\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    await Task.WhenAll(
        SteadyBrowsing(cts.Token),
        OrderBursts(cts.Token),
        ErrorInjection(cts.Token),
        HeavyLoadPhase(cts.Token),
        StockDepletion(cts.Token)
    );
}
catch (OperationCanceledException) { }

Console.WriteLine("Traffic Simulator stopped.");

async Task SteadyBrowsing(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            var response = await client.GetAsync("/api/products", ct);
            Console.WriteLine($"[Browse] GET /api/products -> {(int)response.StatusCode}");

            await Task.Delay(random.Next(500, 1000), ct);

            var productId = random.Next(1, 11);
            response = await client.GetAsync($"/api/products/{productId}", ct);
            Console.WriteLine($"[Browse] GET /api/products/{productId} -> {(int)response.StatusCode}");

            await Task.Delay(random.Next(1000, 2000), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        catch (Exception ex) { Console.WriteLine($"[Browse] Error: {ex.Message}"); await Task.Delay(1000); }
    }
}

async Task OrderBursts(CancellationToken ct)
{
    var customerNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Hank" };

    while (!ct.IsCancellationRequested)
    {
        try
        {
            var burstSize = random.Next(5, 11);
            Console.WriteLine($"\n[Burst] Creating {burstSize} orders rapidly...");

            for (int i = 0; i < burstSize; i++)
            {
                var order = new
                {
                    customerName = customerNames[random.Next(customerNames.Length)],
                    productId = random.Next(1, 11),
                    quantity = random.Next(1, 4)
                };

                var response = await client.PostAsJsonAsync("/api/orders", order, ct);
                Console.WriteLine($"[Burst] POST /api/orders ({order.customerName}, product {order.productId}) -> {(int)response.StatusCode}");
                await Task.Delay(random.Next(50, 200), ct);
            }

            var pause = random.Next(10000, 20000);
            Console.WriteLine($"[Burst] Pausing {pause / 1000}s before next burst...\n");
            await Task.Delay(pause, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        catch (Exception ex) { Console.WriteLine($"[Burst] Error: {ex.Message}"); await Task.Delay(2000); }
    }
}

async Task ErrorInjection(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            // Request non-existent product
            var response = await client.GetAsync("/api/products/999", ct);
            Console.WriteLine($"[Error] GET /api/products/999 -> {(int)response.StatusCode}");
            await Task.Delay(random.Next(2000, 4000), ct);

            // Request non-existent order
            response = await client.GetAsync("/api/orders/999", ct);
            Console.WriteLine($"[Error] GET /api/orders/999 -> {(int)response.StatusCode}");
            await Task.Delay(random.Next(2000, 4000), ct);

            // Try to cancel a potentially shipped order
            var orderId = random.Next(1, 20);
            response = await client.PostAsync($"/api/orders/{orderId}/cancel", null, ct);
            Console.WriteLine($"[Error] POST /api/orders/{orderId}/cancel -> {(int)response.StatusCode}");
            await Task.Delay(random.Next(3000, 5000), ct);

            // Order with invalid product
            var badOrder = new { customerName = "ErrorBot", productId = 999, quantity = 1 };
            response = await client.PostAsJsonAsync("/api/orders", badOrder, ct);
            Console.WriteLine($"[Error] POST /api/orders (bad product) -> {(int)response.StatusCode}");
            await Task.Delay(random.Next(3000, 5000), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        catch (Exception ex) { Console.WriteLine($"[Error] Error: {ex.Message}"); await Task.Delay(2000); }
    }
}

async Task HeavyLoadPhase(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            // Wait before heavy load phase
            await Task.Delay(random.Next(45000, 75000), ct);

            Console.WriteLine("\n[Load] === HEAVY LOAD PHASE START ===");
            var tasks = new List<Task>();

            for (int i = 0; i < 25; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 5; j++)
                    {
                        try
                        {
                            await client.GetAsync("/api/products", ct);
                            await client.GetAsync($"/api/products/{random.Next(1, 11)}", ct);
                            await client.GetAsync("/api/orders?page=1&pageSize=5", ct);
                        }
                        catch { }
                    }
                }, ct));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("[Load] === HEAVY LOAD PHASE END ===\n");

            // Cooldown
            await Task.Delay(60000, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        catch (Exception ex) { Console.WriteLine($"[Load] Error: {ex.Message}"); await Task.Delay(5000); }
    }
}

async Task StockDepletion(CancellationToken ct)
{
    var targetProductId = 1; // Deplete product 1 specifically

    while (!ct.IsCancellationRequested)
    {
        try
        {
            var order = new
            {
                customerName = "StockDepleter",
                productId = targetProductId,
                quantity = 3
            };

            var response = await client.PostAsJsonAsync("/api/orders", order, ct);
            Console.WriteLine($"[Stock] POST /api/orders (product {targetProductId}, qty 3) -> {(int)response.StatusCode}");

            if ((int)response.StatusCode == 400)
            {
                // Product likely out of stock, switch to another
                targetProductId = (targetProductId % 10) + 1;
                Console.WriteLine($"[Stock] Switching to product {targetProductId}");
            }

            await Task.Delay(random.Next(5000, 10000), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        catch (Exception ex) { Console.WriteLine($"[Stock] Error: {ex.Message}"); await Task.Delay(2000); }
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build DemoPrometheusGrafana.sln
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/TrafficSimulator/Program.cs
git commit -m "feat: add traffic simulator with 5 concurrent load patterns"
```

---

### Task 12: Dockerfiles

**Files:**
- Create: `src/DemoApi/Dockerfile`
- Create: `src/TrafficSimulator/Dockerfile`

- [ ] **Step 1: Create DemoApi Dockerfile**

Write to `src/DemoApi/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/DemoApi/DemoApi.csproj src/DemoApi/
RUN dotnet restore src/DemoApi/DemoApi.csproj
COPY src/DemoApi/ src/DemoApi/
RUN dotnet publish src/DemoApi/DemoApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DemoApi.dll"]
```

- [ ] **Step 2: Create TrafficSimulator Dockerfile**

Write to `src/TrafficSimulator/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/TrafficSimulator/TrafficSimulator.csproj src/TrafficSimulator/
RUN dotnet restore src/TrafficSimulator/TrafficSimulator.csproj
COPY src/TrafficSimulator/ src/TrafficSimulator/
RUN dotnet publish src/TrafficSimulator/TrafficSimulator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TrafficSimulator.dll"]
```

- [ ] **Step 3: Commit**

```bash
git add src/DemoApi/Dockerfile src/TrafficSimulator/Dockerfile
git commit -m "feat: add Dockerfiles for DemoApi and TrafficSimulator"
```

---

### Task 13: Prometheus & Grafana Configuration

**Files:**
- Create: `prometheus/prometheus.yml`
- Create: `grafana/provisioning/datasources/prometheus.yml`
- Create: `grafana/provisioning/dashboards/dashboards.yml`

- [ ] **Step 1: Create Prometheus config**

Write to `prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 5s
  evaluation_interval: 5s

scrape_configs:
  - job_name: "demoapi"
    static_configs:
      - targets: ["demoapi:8080"]
```

- [ ] **Step 2: Create Grafana datasource provisioning**

Write to `grafana/provisioning/datasources/prometheus.yml`:

```yaml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    uid: prometheus
```

- [ ] **Step 3: Create Grafana dashboard provisioning config**

Write to `grafana/provisioning/dashboards/dashboards.yml`:

```yaml
apiVersion: 1
providers:
  - name: "default"
    orgId: 1
    folder: ""
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    options:
      path: /var/lib/grafana/dashboards
```

- [ ] **Step 4: Commit**

```bash
git add prometheus/ grafana/provisioning/
git commit -m "feat: add Prometheus scrape config and Grafana provisioning"
```

---

### Task 14: Grafana Dashboards

**Files:**
- Create: `grafana/dashboards/http-overview.json`
- Create: `grafana/dashboards/business-metrics.json`
- Create: `grafana/dashboards/system-database.json`

**Note:** These JSON files use the metric names from prometheus-net's `UseHttpMetrics()` middleware: `http_request_duration_seconds` (histogram with labels `code`, `method`, `controller`, `action`) and `http_requests_received_total` (counter with same labels). After the first run, verify actual metric names at `http://localhost:5000/metrics` and adjust queries if needed.

- [ ] **Step 1: Create HTTP Overview dashboard**

Write to `grafana/dashboards/http-overview.json`:

```json
{
  "annotations": { "list": [] },
  "editable": true,
  "graphTooltip": 1,
  "links": [],
  "panels": [
    {
      "id": 1,
      "type": "timeseries",
      "title": "Request Rate by Endpoint",
      "gridPos": { "h": 8, "w": 12, "x": 0, "y": 0 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "sum(rate(http_request_duration_seconds_count{job=\"demoapi\"}[1m])) by (action)",
          "legendFormat": "{{action}}",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "custom": { "drawStyle": "line", "fillOpacity": 10, "lineWidth": 2, "pointSize": 5, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "reqps"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 2,
      "type": "timeseries",
      "title": "Response Time Percentiles",
      "gridPos": { "h": 8, "w": 12, "x": 12, "y": 0 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "histogram_quantile(0.50, sum(rate(http_request_duration_seconds_bucket{job=\"demoapi\"}[1m])) by (le))",
          "legendFormat": "p50",
          "refId": "A"
        },
        {
          "expr": "histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket{job=\"demoapi\"}[1m])) by (le))",
          "legendFormat": "p95",
          "refId": "B"
        },
        {
          "expr": "histogram_quantile(0.99, sum(rate(http_request_duration_seconds_bucket{job=\"demoapi\"}[1m])) by (le))",
          "legendFormat": "p99",
          "refId": "C"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "custom": { "drawStyle": "line", "fillOpacity": 10, "lineWidth": 2, "pointSize": 5, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "s"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 3,
      "type": "timeseries",
      "title": "Error Rate (%)",
      "gridPos": { "h": 8, "w": 8, "x": 0, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "sum(rate(http_request_duration_seconds_count{job=\"demoapi\",code=~\"[45]..\"}[1m])) / sum(rate(http_request_duration_seconds_count{job=\"demoapi\"}[1m])) * 100",
          "legendFormat": "Error %",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "fixedColor": "red", "mode": "fixed" },
          "custom": { "drawStyle": "line", "fillOpacity": 20, "lineWidth": 2, "pointSize": 5, "showPoints": "never", "thresholdsStyle": { "mode": "line" } },
          "unit": "percent",
          "thresholds": { "mode": "absolute", "steps": [{ "color": "green", "value": null }, { "color": "red", "value": 5 }] }
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 4,
      "type": "gauge",
      "title": "Requests In Progress",
      "gridPos": { "h": 8, "w": 4, "x": 8, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "http_requests_in_progress{job=\"demoapi\"}",
          "legendFormat": "In Progress",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "thresholds" },
          "thresholds": { "mode": "absolute", "steps": [{ "color": "green", "value": null }, { "color": "yellow", "value": 5 }, { "color": "red", "value": 15 }] },
          "min": 0,
          "max": 30
        },
        "overrides": []
      },
      "options": { "reduceOptions": { "calcs": ["lastNotNull"] } }
    },
    {
      "id": 5,
      "type": "piechart",
      "title": "Status Code Distribution",
      "gridPos": { "h": 8, "w": 6, "x": 12, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "sum(increase(http_request_duration_seconds_count{job=\"demoapi\"}[5m])) by (code)",
          "legendFormat": "{{code}}",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": { "color": { "mode": "palette-classic" } },
        "overrides": []
      },
      "options": {
        "legend": { "displayMode": "table", "placement": "right" },
        "pieType": "donut",
        "reduceOptions": { "calcs": ["lastNotNull"] }
      }
    },
    {
      "id": 6,
      "type": "barchart",
      "title": "Top Slowest Endpoints (avg)",
      "gridPos": { "h": 8, "w": 6, "x": 18, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "topk(5, sum(rate(http_request_duration_seconds_sum{job=\"demoapi\"}[5m])) by (action) / sum(rate(http_request_duration_seconds_count{job=\"demoapi\"}[5m])) by (action))",
          "legendFormat": "{{action}}",
          "refId": "A",
          "instant": true
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "unit": "s"
        },
        "overrides": []
      },
      "options": {
        "orientation": "horizontal",
        "xTickLabelRotation": 0
      }
    }
  ],
  "refresh": "5s",
  "schemaVersion": 39,
  "tags": ["demo", "http"],
  "templating": { "list": [] },
  "time": { "from": "now-15m", "to": "now" },
  "timepicker": {},
  "timezone": "browser",
  "title": "HTTP Overview",
  "uid": "http-overview",
  "version": 1
}
```

- [ ] **Step 2: Create Business Metrics dashboard**

Write to `grafana/dashboards/business-metrics.json`:

```json
{
  "annotations": { "list": [] },
  "editable": true,
  "graphTooltip": 1,
  "links": [],
  "panels": [
    {
      "id": 1,
      "type": "timeseries",
      "title": "Orders Created Rate",
      "gridPos": { "h": 8, "w": 12, "x": 0, "y": 0 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "sum(rate(orders_created_total{job=\"demoapi\"}[1m])) by (status)",
          "legendFormat": "{{status}}",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "custom": { "drawStyle": "line", "fillOpacity": 10, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "ops"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 2,
      "type": "timeseries",
      "title": "Order Processing Duration",
      "gridPos": { "h": 8, "w": 12, "x": 12, "y": 0 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "histogram_quantile(0.50, sum(rate(order_processing_duration_seconds_bucket{job=\"demoapi\"}[1m])) by (le))",
          "legendFormat": "p50",
          "refId": "A"
        },
        {
          "expr": "histogram_quantile(0.95, sum(rate(order_processing_duration_seconds_bucket{job=\"demoapi\"}[1m])) by (le))",
          "legendFormat": "p95",
          "refId": "B"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "custom": { "drawStyle": "line", "fillOpacity": 10, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "s"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 3,
      "type": "timeseries",
      "title": "Pending Orders Queue",
      "gridPos": { "h": 8, "w": 8, "x": 0, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "orders_pending_count{job=\"demoapi\"}",
          "legendFormat": "Pending Orders",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "fixedColor": "orange", "mode": "fixed" },
          "custom": { "drawStyle": "line", "fillOpacity": 30, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "short"
        },
        "overrides": []
      },
      "options": {
        "legend": { "displayMode": "list", "placement": "bottom" },
        "tooltip": { "mode": "single" }
      }
    },
    {
      "id": 4,
      "type": "barchart",
      "title": "Order Outcomes",
      "gridPos": { "h": 8, "w": 8, "x": 8, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "sum(increase(orders_processed_total{job=\"demoapi\"}[5m])) by (outcome)",
          "legendFormat": "{{outcome}}",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": { "color": { "mode": "palette-classic" } },
        "overrides": [
          { "matcher": { "id": "byName", "options": "shipped" }, "properties": [{ "id": "color", "value": { "fixedColor": "green", "mode": "fixed" } }] },
          { "matcher": { "id": "byName", "options": "failed" }, "properties": [{ "id": "color", "value": { "fixedColor": "red", "mode": "fixed" } }] }
        ]
      },
      "options": {
        "stacking": "normal",
        "orientation": "vertical"
      }
    },
    {
      "id": 5,
      "type": "stat",
      "title": "Products Out of Stock",
      "gridPos": { "h": 8, "w": 8, "x": 16, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "products_out_of_stock{job=\"demoapi\"}",
          "legendFormat": "Out of Stock",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "thresholds" },
          "thresholds": { "mode": "absolute", "steps": [{ "color": "green", "value": null }, { "color": "yellow", "value": 1 }, { "color": "red", "value": 3 }] }
        },
        "overrides": []
      },
      "options": {
        "reduceOptions": { "calcs": ["lastNotNull"] },
        "colorMode": "background",
        "graphMode": "area"
      }
    },
    {
      "id": 6,
      "type": "timeseries",
      "title": "Stock Replenishment Events",
      "gridPos": { "h": 8, "w": 12, "x": 0, "y": 16 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "sum(rate(stock_replenished_total{job=\"demoapi\"}[1m])) by (product)",
          "legendFormat": "{{product}}",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "custom": { "drawStyle": "bars", "fillOpacity": 80, "lineWidth": 1, "showPoints": "never" },
          "unit": "ops"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["sum"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 7,
      "type": "histogram",
      "title": "Order Value Distribution",
      "gridPos": { "h": 8, "w": 12, "x": 12, "y": 16 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "sum(increase(order_total_amount_bucket{job=\"demoapi\"}[5m])) by (le)",
          "legendFormat": "{{le}}",
          "refId": "A",
          "format": "heatmap"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "unit": "currencyUSD"
        },
        "overrides": []
      },
      "options": {
        "bucketOffset": 0,
        "combine": false,
        "fillOpacity": 80
      }
    }
  ],
  "refresh": "5s",
  "schemaVersion": 39,
  "tags": ["demo", "business"],
  "templating": { "list": [] },
  "time": { "from": "now-15m", "to": "now" },
  "timepicker": {},
  "timezone": "browser",
  "title": "Business Metrics",
  "uid": "business-metrics",
  "version": 1
}
```

- [ ] **Step 3: Create System & Database dashboard**

Write to `grafana/dashboards/system-database.json`:

```json
{
  "annotations": { "list": [] },
  "editable": true,
  "graphTooltip": 1,
  "links": [],
  "panels": [
    {
      "id": 1,
      "type": "timeseries",
      "title": "CPU Usage",
      "gridPos": { "h": 8, "w": 12, "x": 0, "y": 0 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "rate(process_cpu_seconds_total{job=\"demoapi\"}[1m])",
          "legendFormat": "CPU",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "fixedColor": "blue", "mode": "fixed" },
          "custom": { "drawStyle": "line", "fillOpacity": 20, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "percentunit"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "single" }
      }
    },
    {
      "id": 2,
      "type": "timeseries",
      "title": "Memory (Working Set)",
      "gridPos": { "h": 8, "w": 12, "x": 12, "y": 0 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "process_working_set_bytes{job=\"demoapi\"}",
          "legendFormat": "Working Set",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "fixedColor": "purple", "mode": "fixed" },
          "custom": { "drawStyle": "line", "fillOpacity": 20, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "bytes"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "single" }
      }
    },
    {
      "id": 3,
      "type": "timeseries",
      "title": "GC Collections by Generation",
      "gridPos": { "h": 8, "w": 12, "x": 0, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "rate(dotnet_collection_count_total{job=\"demoapi\"}[1m])",
          "legendFormat": "Gen {{generation}}",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "custom": { "drawStyle": "line", "fillOpacity": 10, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "ops"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 4,
      "type": "timeseries",
      "title": "Thread Pool Threads",
      "gridPos": { "h": 8, "w": 12, "x": 12, "y": 8 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "process_num_threads{job=\"demoapi\"}",
          "legendFormat": "Threads",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "fixedColor": "green", "mode": "fixed" },
          "custom": { "drawStyle": "line", "fillOpacity": 10, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "short"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "single" }
      }
    },
    {
      "id": 5,
      "type": "timeseries",
      "title": "DB Query Duration by Type",
      "gridPos": { "h": 8, "w": 8, "x": 0, "y": 16 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "histogram_quantile(0.95, sum(rate(db_query_duration_seconds_bucket{job=\"demoapi\"}[1m])) by (le, query_type))",
          "legendFormat": "p95 {{query_type}}",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "mode": "palette-classic" },
          "custom": { "drawStyle": "line", "fillOpacity": 10, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "s"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "multi", "sort": "desc" }
      }
    },
    {
      "id": 6,
      "type": "timeseries",
      "title": "DB Error Rate",
      "gridPos": { "h": 8, "w": 8, "x": 8, "y": 16 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "rate(db_errors_total{job=\"demoapi\"}[1m])",
          "legendFormat": "Errors/s",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "fixedColor": "red", "mode": "fixed" },
          "custom": { "drawStyle": "line", "fillOpacity": 20, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "ops"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "single" }
      }
    },
    {
      "id": 7,
      "type": "timeseries",
      "title": "Managed Heap Size",
      "gridPos": { "h": 8, "w": 8, "x": 16, "y": 16 },
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "targets": [
        {
          "expr": "dotnet_total_memory_bytes{job=\"demoapi\"}",
          "legendFormat": "Heap Size",
          "refId": "A"
        }
      ],
      "fieldConfig": {
        "defaults": {
          "color": { "fixedColor": "orange", "mode": "fixed" },
          "custom": { "drawStyle": "line", "fillOpacity": 20, "lineWidth": 2, "showPoints": "never", "lineInterpolation": "smooth" },
          "unit": "bytes"
        },
        "overrides": []
      },
      "options": {
        "legend": { "calcs": ["mean", "max"], "displayMode": "table", "placement": "bottom" },
        "tooltip": { "mode": "single" }
      }
    }
  ],
  "refresh": "5s",
  "schemaVersion": 39,
  "tags": ["demo", "system"],
  "templating": { "list": [] },
  "time": { "from": "now-15m", "to": "now" },
  "timepicker": {},
  "timezone": "browser",
  "title": "System & Database",
  "uid": "system-database",
  "version": 1
}
```

- [ ] **Step 4: Commit**

```bash
git add grafana/dashboards/
git commit -m "feat: add pre-provisioned Grafana dashboards"
```

---

### Task 15: Docker Compose

**Files:**
- Create: `docker-compose.yml`

- [ ] **Step 1: Create docker-compose.yml**

Write to `docker-compose.yml`:

```yaml
version: "3.8"

services:
  demoapi:
    build:
      context: .
      dockerfile: src/DemoApi/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    networks:
      - demo-network

  trafficsimulator:
    build:
      context: .
      dockerfile: src/TrafficSimulator/Dockerfile
    environment:
      - API_BASE_URL=http://demoapi:8080
    depends_on:
      - demoapi
    restart: on-failure
    networks:
      - demo-network

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
    depends_on:
      - demoapi
    networks:
      - demo-network

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_AUTH_ANONYMOUS_ENABLED=true
      - GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer
    volumes:
      - ./grafana/provisioning:/etc/grafana/provisioning:ro
      - ./grafana/dashboards:/var/lib/grafana/dashboards:ro
    depends_on:
      - prometheus
    networks:
      - demo-network

networks:
  demo-network:
    driver: bridge
```

- [ ] **Step 2: Commit**

```bash
git add docker-compose.yml
git commit -m "feat: add Docker Compose orchestration for full demo stack"
```

---

### Task 16: End-to-End Verification

- [ ] **Step 1: Build and start the full stack**

```bash
docker-compose up --build -d
```

Expected: All 4 services start without errors.

- [ ] **Step 2: Wait ~10s for services to initialize, then verify DemoApi**

```bash
curl http://localhost:5000/health
```

Expected: `Healthy` (200 OK)

- [ ] **Step 3: Verify metrics endpoint**

```bash
curl http://localhost:5000/metrics
```

Expected: Prometheus text format output containing `http_request_duration_seconds`, `orders_created_total`, `db_query_duration_seconds`, `process_cpu_seconds_total`, etc.

**Important:** Check the actual metric names and label names in the output. If prometheus-net uses different names than what the Grafana dashboards expect (e.g., `http_requests_received_total` instead of `http_request_duration_seconds_count`), update the PromQL queries in the dashboard JSON files accordingly.

- [ ] **Step 4: Verify Prometheus is scraping**

Open `http://localhost:9090/targets` in a browser. The `demoapi` target should show as `UP`.

- [ ] **Step 5: Verify Grafana dashboards**

Open `http://localhost:3000` in a browser. Login with admin/admin (or view anonymously). Three dashboards should appear: "HTTP Overview", "Business Metrics", "System & Database". Panels should start showing data within 30-60 seconds of the traffic simulator running.

- [ ] **Step 6: Verify traffic simulator is generating traffic**

```bash
docker-compose logs -f trafficsimulator
```

Expected: Console output showing `[Browse]`, `[Burst]`, `[Error]`, `[Load]`, `[Stock]` pattern activity.

- [ ] **Step 7: Commit any fixes from verification**

If any dashboard queries or configuration needed adjustments, commit the fixes:

```bash
git add -A
git commit -m "fix: adjust metric names and dashboard queries after E2E verification"
```

- [ ] **Step 8: Final cleanup — add .gitignore**

Write to `.gitignore`:

```
bin/
obj/
*.db
.vs/
*.user
```

```bash
git add .gitignore
git commit -m "chore: add .gitignore"
```
