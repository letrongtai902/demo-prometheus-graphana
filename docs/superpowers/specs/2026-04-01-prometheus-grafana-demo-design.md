# Prometheus + Grafana Demo — Design Spec

## Overview

A C# demo project showcasing Prometheus metrics collection and Grafana visualization. The system simulates an e-commerce order API with background services, a traffic simulator, and pre-provisioned Grafana dashboards — all orchestrated via Docker Compose for a single-command startup.

## Architecture

Four runtime components:

| Component | Type | Purpose |
|---|---|---|
| **DemoApi** | ASP.NET Core Web API (.NET 8) | Monitored application — REST endpoints + background services + SQLite |
| **TrafficSimulator** | .NET 8 Console App | Generates HTTP traffic patterns against DemoApi |
| **Prometheus** | `prom/prometheus:latest` | Scrapes `/metrics` from DemoApi every 5s |
| **Grafana** | `grafana/grafana:latest` | Pre-provisioned dashboards querying Prometheus |

```
TrafficSimulator ──HTTP──> DemoApi <──scrape── Prometheus <──query── Grafana
                              |                                        |
                           SQLite                              localhost:3000
                              |
                         /metrics endpoint
```

All components run as Docker Compose services. `docker-compose up` starts everything.

## DemoApi — Domain Model

### Entities (SQLite via EF Core)

**Product:**
- Id (int, PK)
- Name (string)
- Price (decimal)
- Stock (int)

**Order:**
- Id (int, PK)
- CustomerName (string)
- Total (decimal)
- Status (enum: Pending, Processing, Shipped, Failed, Cancelled)
- CreatedAt (DateTime)

### REST Endpoints

| Method | Route | Behavior |
|---|---|---|
| GET | `/api/products` | List all products |
| GET | `/api/products/{id}` | Get product by id (404 if not found) |
| POST | `/api/orders` | Create order. Body: `{ "customerName": "...", "productId": 1, "quantity": 2 }`. Validates stock, returns 400 if out of stock. |
| GET | `/api/orders` | List orders (supports `?page=1&pageSize=10`) |
| GET | `/api/orders/{id}` | Get order by id (404 if not found) |
| POST | `/api/orders/{id}/cancel` | Cancel order (400 if already shipped/failed) |
| GET | `/health` | Health check |
| GET | `/metrics` | Prometheus scrape endpoint |

### Background Services

| Service | Behavior |
|---|---|
| **OrderProcessorService** | Every 3-5s, picks up pending orders. Transitions: pending -> processing -> shipped (90%) or failed (10%). Simulates variable processing time (100ms-2s). |
| **StockReplenishmentService** | Every 30s, checks products with stock < 5 and restocks to 50. |
| **SystemMetricsCollector** | Every 5s, collects process-level metrics (CPU, memory, GC, threads). |

### Seed Data

On startup, seed 10 products with names, prices ($5-$100 range), and initial stock of 50 each.

## Metrics Catalog

### HTTP Metrics (via prometheus-net.AspNetCore middleware)

- `http_request_duration_seconds` — histogram by method, route, status code
- `http_requests_in_progress` — gauge of concurrent requests
- `http_requests_total` — counter by method, route, status code

### Business Metrics (custom, via prometheus-net)

- `orders_created_total` — counter, labels: `status` (success/failed)
- `orders_processed_total` — counter, labels: `outcome` (shipped/failed)
- `order_processing_duration_seconds` — histogram
- `orders_pending_count` — gauge
- `order_total_amount` — histogram (buckets: 10, 25, 50, 100, 250, 500)
- `stock_replenished_total` — counter, labels: `product`
- `products_out_of_stock` — gauge

### Database Metrics (custom)

- `db_query_duration_seconds` — histogram, labels: `query_type` (select/insert/update)
- `db_errors_total` — counter

### System/Process Metrics (built-in from prometheus-net + custom)

- `process_cpu_seconds_total` — CPU usage
- `process_working_set_bytes` — memory usage
- `dotnet_gc_collection_count` — GC collections by generation
- `dotnet_threadpool_threads_count` — thread pool threads
- `dotnet_gc_heap_size_bytes` — managed heap size

Total: ~18 distinct metrics across 4 categories.

## Traffic Simulator

Separate .NET 8 console app using HttpClient against `http://demoapi:8080`.

### Traffic Patterns (run concurrently as async tasks)

| Pattern | Behavior | Grafana effect |
|---|---|---|
| **Steady browsing** | GET `/api/products` and `/api/products/{random_id}` every 1-2s | Stable baseline request rate |
| **Order bursts** | POST 5-10 orders rapidly, then pause 10-20s | Spike patterns in request rate & latency |
| **Error injection** | GET non-existent products/orders, cancel shipped orders, every 3-5s | Error rate spikes, 4xx status codes |
| **Heavy load phase** | 20+ concurrent requests for ~30s, then 60s cooldown | Latency degradation, in-progress gauge spikes |
| **Stock depletion** | Repeatedly order same product until out of stock, every 5-10s | Out-of-stock gauge rising, order failures |

All patterns run in a continuous loop with randomized intervals. Console output logs each action for presenter narration.

### Startup Behavior

Wait for DemoApi health check to return 200 before starting traffic. Retry with 2s backoff.

## Grafana Dashboards

Pre-provisioned via file-based provisioning. All dashboards auto-refresh every 5s.

### Dashboard 1: HTTP Overview

- Request rate (req/s) by endpoint — time series
- Response time percentiles (p50, p95, p99) — time series
- Error rate (%) — time series with threshold line
- Requests in progress — gauge panel
- Status code distribution — pie chart
- Top slowest endpoints — bar chart

### Dashboard 2: Business Metrics

- Orders created rate — time series
- Order processing duration (p50, p95) — time series
- Pending orders queue depth — time series
- Order outcomes (shipped vs failed) — stacked bar
- Products out of stock — stat panel
- Stock replenishment events — time series
- Order value distribution — histogram

### Dashboard 3: System & Database

- CPU usage — time series
- Memory (working set) — time series
- GC collections by generation — time series
- Thread pool threads — time series
- DB query duration by type — time series
- DB error rate — time series
- Managed heap size — time series

## Project Structure

```
demo-prometheus-graphana/
├── docker-compose.yml
├── DemoPrometheusGrafana.sln
├── src/
│   ├── DemoApi/
│   │   ├── DemoApi.csproj
│   │   ├── Dockerfile
│   │   ├── Program.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── SeedData.cs
│   │   ├── Models/
│   │   │   ├── Product.cs
│   │   │   └── Order.cs
│   │   ├── Endpoints/
│   │   │   ├── ProductEndpoints.cs
│   │   │   └── OrderEndpoints.cs
│   │   ├── Services/
│   │   │   ├── OrderProcessorService.cs
│   │   │   ├── StockReplenishmentService.cs
│   │   │   └── SystemMetricsCollector.cs
│   │   └── Metrics/
│   │       └── AppMetrics.cs
│   └── TrafficSimulator/
│       ├── TrafficSimulator.csproj
│       ├── Dockerfile
│       └── Program.cs
├── prometheus/
│   └── prometheus.yml
└── grafana/
    ├── provisioning/
    │   ├── datasources/
    │   │   └── prometheus.yml
    │   └── dashboards/
    │       └── dashboards.yml
    └── dashboards/
        ├── http-overview.json
        ├── business-metrics.json
        └── system-database.json
```

## Docker Compose

| Service | Image | Ports | Notes |
|---|---|---|---|
| `demoapi` | Built from `src/DemoApi/Dockerfile` | `5000:8080` | Exposes API and metrics |
| `trafficsimulator` | Built from `src/TrafficSimulator/Dockerfile` | none | Depends on demoapi health |
| `prometheus` | `prom/prometheus:latest` | `9090:9090` | Scrapes demoapi:8080/metrics every 5s |
| `grafana` | `grafana/grafana:latest` | `3000:3000` | Auto-provisions datasource + dashboards |

### Key Configuration

- Prometheus config: scrape `demoapi:8080/metrics` every 5s
- Grafana datasource provisioning: points to `http://prometheus:9090`
- Grafana dashboard provisioning: loads JSON files from `/var/lib/grafana/dashboards`
- Grafana default credentials: admin/admin
- Traffic simulator: `depends_on` demoapi with health check condition
- SQLite DB is ephemeral (inside container, resets on restart)

## NuGet Packages

### DemoApi
- `Microsoft.EntityFrameworkCore.Sqlite` — SQLite provider
- `prometheus-net.AspNetCore` — Prometheus metrics + HTTP middleware

### TrafficSimulator
- No external packages needed (uses built-in `System.Net.Http`)

## Target Framework

.NET 8 (LTS)
