# Prometheus & Grafana Demo (.NET)

A complete demo project showcasing Prometheus metrics collection and Grafana visualization with a .NET 9 API, background services, traffic simulation, and alerting.

## Architecture

```
┌──────────────────┐     ┌─────────────────┐     ┌─────────────┐
│   DemoApi        │────▶│   Prometheus     │────▶│   Grafana   │
│   :5000          │     │   :9090          │     │   :3000     │
│   (metrics)      │     │   (scrape 5s)    │     │ (dashboards)│
└──────────────────┘     └─────────────────┘     └─────────────┘
        ▲
        │
┌───────┴──────────┐
│ Traffic Simulator │
│ (5 patterns)      │
└──────────────────┘
```

## Quick Start

```bash
docker compose up -d --build
```

| Service    | URL                          |
|------------|------------------------------|
| DemoApi    | http://localhost:5000         |
| Prometheus | http://localhost:9090         |
| Grafana    | http://localhost:3000         |

Grafana credentials: `admin` / `admin` (anonymous read access is also enabled).

## Components

### DemoApi

ASP.NET Core Minimal API with EF Core (SQLite) exposing:

| Endpoint                      | Method | Description                          |
|-------------------------------|--------|--------------------------------------|
| `/api/products`               | GET    | List all products                    |
| `/api/products/{id}`          | GET    | Get product by ID                    |
| `/api/orders`                 | GET    | List orders (paginated)              |
| `/api/orders/{id}`            | GET    | Get order by ID                      |
| `/api/orders`                 | POST   | Create an order                      |
| `/api/orders/{id}/cancel`     | POST   | Cancel an order                      |
| `/api/simulate-error`         | GET    | Throws exception (returns 500)       |
| `/health`                     | GET    | Health check                         |
| `/metrics`                    | GET    | Prometheus metrics endpoint          |

**Background Services:**

- **OrderProcessorService** - Processes pending orders every 3-6s (90% shipped, 10% failed)
- **StockReplenishmentService** - Restocks products below 5 units every 30s
- **SystemMetricsCollector** - Updates gauge metrics (pending orders, out-of-stock) every 5s

### Traffic Simulator

Runs inside Docker alongside the API, generating five concurrent traffic patterns:

| Pattern            | Description                                          |
|--------------------|------------------------------------------------------|
| Steady Browsing    | Continuous GET requests to products (0.5-2s interval) |
| Order Bursts       | 5-10 rapid orders, then 10-20s pause                 |
| Error Injection    | Requests to invalid IDs (404s, 400s) every 2-5s      |
| Heavy Load Phase   | 25 concurrent tasks, every 45-75s                    |
| Stock Depletion    | Drains product stock by ordering 3 units at a time   |

### ErrorTrigger (Local Console App)

Sends requests to `/api/simulate-error` every 2 seconds to generate 500 errors. Used to test Grafana alerting.

```bash
cd src/ErrorTrigger
dotnet run
```

## Metrics

### HTTP Metrics (prometheus-net)

Automatically collected by `UseHttpMetrics()`:

- `http_request_duration_seconds` (histogram) - Request duration by method, endpoint, and status code
- `http_requests_in_progress` (gauge) - Currently active requests

### Custom Business Metrics

| Metric                             | Type      | Labels          | Description                      |
|------------------------------------|-----------|-----------------|----------------------------------|
| `orders_created_total`             | Counter   | status          | Orders created (success/failed)  |
| `orders_processed_total`           | Counter   | outcome         | Orders processed (shipped/failed)|
| `order_processing_duration_seconds`| Histogram | -               | Time to process an order         |
| `orders_pending_count`             | Gauge     | -               | Current pending orders           |
| `order_total_amount`               | Histogram | -               | Order value distribution         |
| `stock_replenished_total`          | Counter   | product         | Stock replenishment events       |
| `products_out_of_stock`            | Gauge     | -               | Products with zero stock         |
| `db_query_duration_seconds`        | Histogram | query_type      | Database query duration          |
| `db_errors_total`                  | Counter   | -               | Database errors                  |

## Grafana Dashboards

Three pre-provisioned dashboards are loaded automatically:

### HTTP Overview

- Request Rate by Endpoint
- Error Rate by Endpoint (4xx/5xx per endpoint)
- Response Time Percentiles (p50, p95, p99)
- Error Rate (%)
- Requests In Progress (gauge)
- Status Code Distribution (donut chart)
- Top Slowest Endpoints
- Errors by Endpoint (table with method, endpoint, status code, count)

### Business Metrics

- Orders Created Rate
- Order Processing Duration (p50, p95)
- Pending Orders Queue
- Order Outcomes (shipped vs failed)
- Products Out of Stock
- Stock Replenishment Events
- Order Value Distribution

### System & Database

- CPU Usage
- Memory (Working Set)
- GC Collections by Generation
- Thread Pool Threads
- DB Query Duration by Type (p95)
- DB Error Rate
- Managed Heap Size

## Alerting

A pre-provisioned alert rule monitors for 5xx errors:

- **Rule:** `Server Error (5xx) Detected`
- **Query:** `increase(http_request_duration_seconds_count{code=~"5.."}[1m]) > 0`
- **Evaluation:** Every 10 seconds
- **Severity:** Critical

**To test alerting:**

1. Start the ErrorTrigger: `cd src/ErrorTrigger && dotnet run`
2. Open Grafana Alerting: http://localhost:3000/alerting
3. The alert transitions from **Normal** to **Firing** within ~10 seconds
4. Stop the ErrorTrigger (Ctrl+C) - alert returns to **Normal** after ~1 minute

## Project Structure

```
├── docker-compose.yml
├── prometheus/
│   └── prometheus.yml              # Scrape config (5s interval)
├── grafana/
│   ├── dashboards/                 # Dashboard JSON definitions
│   │   ├── http-overview.json
│   │   ├── business-metrics.json
│   │   └── system-database.json
│   └── provisioning/
│       ├── alerting/alerts.yml     # Alert rules
│       ├── dashboards/dashboards.yml
│       └── datasources/prometheus.yml
└── src/
    ├── DemoApi/
    │   ├── Program.cs              # App setup, middleware, endpoints
    │   ├── Dockerfile
    │   ├── Models/                 # Product, Order entities
    │   ├── Data/                   # DbContext, seed data
    │   ├── Endpoints/              # Product & Order endpoint groups
    │   ├── Metrics/                # Custom Prometheus metrics
    │   └── Services/               # Background processing services
    ├── TrafficSimulator/
    │   ├── Program.cs              # 5 concurrent traffic patterns
    │   └── Dockerfile
    └── ErrorTrigger/
        └── Program.cs              # 500 error generator for alerting
```

## Cleanup

```bash
docker compose down
```
