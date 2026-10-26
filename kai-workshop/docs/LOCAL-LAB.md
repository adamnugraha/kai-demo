# Local or VPS rehearsal

Use Linux x86-64 with Docker Engine and the Compose plugin. A planning allowance is 2 vCPU, 4 GB RAM and 10 GB free disk for the small SQLite lab, or 4 vCPU, 8 GB RAM and 20 GB free disk when adding monitoring. These are starting estimates, not tested capacity guarantees. SQL Server needs additional resources. Keep this lab separate from existing MSP workloads.

## Start

```bash
python3 ops/new-env.py
# Ensure the .NET container user can write the two data directories.
# On a normal rootful Docker host, the owner can run:
sudo chown -R 1654:1654 data/app data/mock
bash ops/local-start.sh
```

Open http://127.0.0.1:8080 locally or through an SSH tunnel. Copy the generated LAB_TOKEN from `.env` into the page. The token stays in the page memory. Enter synthetic aliases only. Local endpoints use HTTP only on loopback or inside the Compose network.

```bash
python3 ops/lab.py status
python3 ops/lab.py book
python3 ops/lab.py fault-on
python3 ops/lab.py book
python3 ops/lab.py dispatch
python3 ops/lab.py status
python3 ops/lab.py fault-off
# Wait for the pending event retry delay, at most 30 seconds.
python3 ops/lab.py dispatch
python3 ops/lab.py status
```

The dispatcher is deliberately presenter-triggered in this first lab. Pending events survive an application restart. For continuous local dispatch, set RUN_WORKER=true in your override. Cloud Run uses the explicit dispatch job or invocation; do not enable the in-process worker on request-billed Cloud Run services.

## Monitoring

Add a generated GRAFANA_PASSWORD to your local `.env`. Then:

```bash
docker compose -f compose.yaml -f compose.observability.yaml up -d
```

Grafana is http://127.0.0.1:3000, username `admin`. Prometheus is http://127.0.0.1:9090. The dashboard uses an external search probe, so failures that never reach the application still count. The alert is visible in Prometheus's Alerts page. An external notification receiver is not configured in this package.

Allow at least two minutes of traffic before reading one-minute rates. The probe uses synthetic search traffic, so its results do not represent all real booking users. Missing probe data must be treated as unknown, not as 100% success.

Monitoring images use upstream tags. Once rehearsed, record and pin their image digests on 29 October, and avoid pulling new versions on presentation morning.

## No Docker option

Install the .NET 10 SDK. Set LAB_TOKEN locally to a random value at least 24 characters long. In separate terminals:

```bash
dotnet run --project src/RailBooking.Mock -c Release
AUTO_INIT=true dotnet run --project src/RailBooking.Modern -c Release
```

Both processes must inherit the same token. The mock uses port 8082 and the application uses 8080. Use the shell syntax appropriate for Windows if needed. SQLite files are created in the process working directory. Do not run multiple app replicas against a shared SQLite file.

## SQL Server profile

The optional `compose.sqlserver.yaml` uses SQL Server Developer edition for non-production teaching. Read and accept the Microsoft container terms before running it. Set local SQL_SA_PASSWORD and SQL_APP_PASSWORD, start only `sqlserver`, wait for readiness, then use a SQL client on loopback to create database `railbooking`, login `railapp`, and a mapped database user. Temporarily grant schema creation to run `--init-db`, then remove DDL privileges and retain only the table read/write rights required by the app. Use the same SQL Server for the Windows and modern application.

```bash
docker compose -f compose.yaml -f compose.sqlserver.yaml up -d sqlserver
# Owner creates the database/login and grants the initial schema permissions.
docker compose -f compose.yaml -f compose.sqlserver.yaml run --rm app --init-db
docker compose -f compose.yaml -f compose.sqlserver.yaml up -d app mock
```

The sample SQL profile trusts the local server's self-signed certificate. Cloud and production connections should validate the configured server certificate and hostname. This profile does not create the database/login automatically.

## Stop

`docker compose down` stops local services and keeps bind-mounted data. Include the same override files used when starting extra services. Delete data only when intentionally resetting this isolated lab. Never run broad prune commands on a shared server.
