using Prometheus;

namespace DemoApi.Metrics;

public static class AppMetrics
{
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

    public static readonly Histogram DbQueryDuration = Prometheus.Metrics.CreateHistogram(
        "db_query_duration_seconds",
        "Database query duration",
        new HistogramConfiguration { LabelNames = new[] { "query_type" } });

    public static readonly Counter DbErrorsTotal = Prometheus.Metrics.CreateCounter(
        "db_errors_total",
        "Total number of database errors");
}
