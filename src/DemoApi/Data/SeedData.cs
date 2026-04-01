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
