# Readiness and verification

Prepared and checked on 26 October 2026. This is a teaching lab release, pending deployment rehearsal in the chosen account.

| Area | Result | Boundary |
|---|---|---|
| Modern .NET application | Release build passed with no warnings | Linux, .NET SDK 10.0.403 |
| Mock notification service | Release build passed | .NET 10 |
| Legacy MVC application | Release build passed | Reference-assembly compilation only, not Windows/IIS execution |
| Shared booking behavior | Nine functional checks passed | SQLite provider |
| Real HTTP application flow | Passed | Local modern app and mock service |
| User failure with healthy liveness | Passed | Separate local faulty and healthy app processes |
| Client probe metrics | Passed | Local HTTP execution and metric format |
| Initialization / dispatch jobs | Entry points passed | Local SQLite execution |
| Python, YAML and JSON | Parsed successfully | Includes CloudFormation YAML, not AWS semantic validation |
| Terraform configuration | Formatting and HCL parsing passed | Provider-schema validation blocked by host Unix-socket restrictions |
| Docker Compose and container images | Prepared | Not built or run in Docker here |
| SQL Server transactions | Integration gate pending | Run against an isolated SQL Server database |
| Windows/IIS migration stages | Integration gate pending | Requires a Windows host |
| GCP / AWS deployment | Not performed | Requires the selected account and authenticated execution |
| Prometheus / Grafana containers | Configuration prepared | Container runtime rehearsal pending |

## Functional checks that passed

1. Same idempotency key returns the original booking.
2. A retry does not consume more seats.
3. Reusing a key with changed input returns a conflict.
4. A booking and its notification event both commit.
5. Failed seat allocation leaves no booking.
6. Concurrent identical requests create one booking.
7. A failed notification event waits before retry.
8. A delivered event no longer appears as pending.
9. Concurrent requests cannot oversell a three-seat test inventory.

The HTTP exercise also verified search, reservation replay, notification failure, backlog recovery, duplicate suppression, rejection of a missing token, the faulty-version response, the healthy-version response, serving the browser page, Prometheus text output, client failure measurement, and explicit job invocation. Browser interaction in a real presentation environment remains a rehearsal step.

## Required checks in the chosen environment

Use an isolated test database. Set TEST_SQL_CONNECTION locally, then run `dotnet run --project tests -c Release`. The SQL Server test creates synthetic rows and exercises concurrent requests. SQL Server can reject transactions under contention; diagnose errors and add bounded client retries with the same key where appropriate. Do not hide failures or count them as successful tests.

Run `terraform validate` and a complete `terraform plan` in Cloud Shell before applying. Check billing, region support, quotas, database encryption, secret versions, service accounts, VPC routes and dependency reachability. The foundation and application use separate state directories.

Build and start the containers, test the dashboard and lab alert, run the Windows migration, and perform the actual cloud traffic rollback. Record evidence in `templates/06-sit-uat-checklist.md` and complete a timed rehearsal on 29 October.

## Presentation decision

Use the full migration demo only after Windows and SQL Server pass. Otherwise use the clearly labelled local behavior simulation and the source-code walkthrough. Use a prerecorded cloud demo if the live environment has not passed its recovery rehearsal. Do not claim 99.9% monthly availability based on this short lab.
