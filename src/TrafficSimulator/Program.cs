using System.Net.Http.Json;

var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
var random = new Random();

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
            var response = await client.GetAsync("/api/products/999", ct);
            Console.WriteLine($"[Error] GET /api/products/999 -> {(int)response.StatusCode}");
            await Task.Delay(random.Next(2000, 4000), ct);

            response = await client.GetAsync("/api/orders/999", ct);
            Console.WriteLine($"[Error] GET /api/orders/999 -> {(int)response.StatusCode}");
            await Task.Delay(random.Next(2000, 4000), ct);

            var orderId = random.Next(1, 20);
            response = await client.PostAsync($"/api/orders/{orderId}/cancel", null, ct);
            Console.WriteLine($"[Error] POST /api/orders/{orderId}/cancel -> {(int)response.StatusCode}");
            await Task.Delay(random.Next(3000, 5000), ct);

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

            await Task.Delay(60000, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        catch (Exception ex) { Console.WriteLine($"[Load] Error: {ex.Message}"); await Task.Delay(5000); }
    }
}

async Task StockDepletion(CancellationToken ct)
{
    var targetProductId = 1;

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
                targetProductId = (targetProductId % 10) + 1;
                Console.WriteLine($"[Stock] Switching to product {targetProductId}");
            }

            await Task.Delay(random.Next(5000, 10000), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        catch (Exception ex) { Console.WriteLine($"[Stock] Error: {ex.Message}"); await Task.Delay(2000); }
    }
}
