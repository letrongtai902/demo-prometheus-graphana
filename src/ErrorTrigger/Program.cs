var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"Error Trigger — Target: {baseUrl}");
Console.WriteLine("This app sends requests to /api/simulate-error to generate 500 errors.");
Console.WriteLine("Watch Grafana Alerting (http://localhost:3000/alerting) for the alert to fire.\n");

var count = 0;

while (true)
{
    try
    {
        var response = await client.GetAsync("/api/simulate-error");
        count++;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Request #{count} -> {(int)response.StatusCode} {response.ReasonPhrase}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connection error: {ex.Message}");
    }

    await Task.Delay(2000);
}
