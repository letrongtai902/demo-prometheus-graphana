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
