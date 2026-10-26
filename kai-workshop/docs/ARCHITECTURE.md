# Architecture views

These diagrams describe the teaching topology. They do not represent KAI's current systems.

## Incremental migration

```mermaid
flowchart TB
  C["Browser and test clients"] --> E["Modern entry point"]
  E -->|Migrated routes| M["ASP.NET Core on .NET 10"]
  E -->|Remaining routes| L["ASP.NET MVC on Windows"]
  M --> D["Shared SQL Server"]
  L --> D
```

The supplied adapter supports only the lab API routes. Both implementations share the booking library. Do not combine the SQLite fallback with the Windows migration path, which requires SQL Server.

## Notification isolation

```mermaid
flowchart TB
  U["User or client probe"] --> A["Cloud booking API"]
  A -->|One transaction| D["SQL Server: booking and outbox"]
  J["Explicit dispatch job"] -->|Read pending events| D
  J -->|Authenticated HTTPS| N["VPS notification mock"]
  N --> X["Durable duplicate suppression"]
  J -->|Mark delivered after success| D
```

The first release invokes dispatch explicitly. A later scheduled worker automates delivery. Pub/Sub and SQS can be introduced between the publisher and consumer after the basic recovery behavior works.

## Monitoring boundary

```mermaid
flowchart TB
  P["VPS client probe"] -->|Search and validate response| A["Application endpoint"]
  P -->|Client outcomes| M["Prometheus"]
  M --> G["Grafana"]
  M --> R["Lab alert rule"]
  A --> L["Structured application logs"]
```

A VPS outage also removes its probe and monitoring. Keep cloud-native logs and platform monitoring available independently. Missing probe data is unknown and requires a monitoring-health alert. These synthetic searches do not measure every customer journey.
