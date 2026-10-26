# KAI application modernization and SRE workshop

**Event:** Friday, 30 October 2026, morning, Asia/Jakarta. Exact start time to be confirmed.
**Audience:** application and infrastructure teams. **Duration:** 120 minutes.
**Application:** fictional RailBooking. All routes, availability and passenger aliases are synthetic.

## Start here

1. Read `docs/ACCESS.md` and choose the primary deployment environment.
2. Rehearse the local fallback using `docs/LOCAL-LAB.md`.
3. Use `docs/WINDOWS-LEGACY.md` for the real .NET Framework application.
4. Follow `docs/GCP.md` for the main cloud lab, or `docs/AWS.md` for the alternative.
5. Run the verification gates in `docs/VALIDATION.md`, then rehearse `docs/DEMO-RUNBOOK.md`.
6. Use the PowerPoint and worksheets in `presentation/` and `templates/`.

## What this package contains

| Component | Implementation |
|---|---|
| Legacy application | ASP.NET MVC 5 on .NET Framework 4.8, Windows/IIS |
| Modern application | ASP.NET Core on .NET 10, Linux container |
| Shared booking rules | .NET Standard library with transactions and idempotency |
| Cloud data path | SQL Server, same schema before and after migration |
| Local fallback | SQLite, one application instance, persistent local file |
| Notification reliability | Transactional SQL outbox and authenticated HTTP dispatcher |
| Mock dependency | Durable duplicate suppression, controlled HTTP failure |
| Observability | Client probe, Prometheus, Grafana dashboard, structured application logs |
| GCP | Optional Cloud SQL/VPC foundation, Cloud Run service and jobs |
| AWS | Fargate/ALB application stack using existing VPC, SQL Server, secrets and certificate |
| Materials | Editable deck, presenter notes, demo runbook and participant worksheets |

The first lab uses a database outbox directly. Pub/Sub and SQS are future extensions, not implemented broker integrations. The small migration route adapter forwards only the demonstrated API routes. Use YARP when expanding to a general application proxy.

## Boundaries

This is a teaching package, not a production railway reservation system. It does not implement payments, identity federation, inventory reconciliation, a complete booking lifecycle, disaster recovery automation or safety-critical rail systems. SQL Server and Windows execution require separate integration checks. Do not treat short tests as proof of monthly availability.

Cloud resources have not been deployed by this package author. Account access, region quotas, costs, networking and a full rehearsal remain deployment gates. `docs/VALIDATION.md` records what was actually verified.

## Preparation schedule

| Date | Completion gate |
|---|---|
| Mon 26 October | Confirm cloud, budget, VPS specifications and Windows option. Build local package. |
| Tue 27 October | Deploy the chosen cloud and VPS components. Run SQL Server and Windows integration tests. |
| Wed 28 October | Run both demos end to end. Record recovery timings and capture a backup recording. |
| Thu 29 October | Freeze image digests and slide content. Full timed rehearsal, preferably twice. |
| Fri 30 October morning | Start 45 minutes before the meeting. Check authentication, DNS, data and rollback. |

Use one live cloud. A second cloud is optional once the first passes all gates. Keep a complete recording and the local fallback available.
