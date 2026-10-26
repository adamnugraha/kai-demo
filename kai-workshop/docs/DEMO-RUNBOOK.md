# Presenter runbook

## 120-minute agenda

| Minutes | Activity |
|---|---|
| 0–10 | Customer journey, success criteria, meaning of 99.9% |
| 10–30 | Modernization principles and incremental .NET migration |
| 30–50 | Demo 1 |
| 50–55 | Architecture and operational ownership |
| 55–60 | Break |
| 60–80 | SLOs, error budgets, observability and safe releases |
| 80–100 | Demo 2 |
| 100–110 | KAI pilot worksheet |
| 110–120 | Questions and next actions |

## Preflight, 45 minutes before the meeting

Verify cloud login and token lifetime, instance/database readiness, notification endpoint TLS, presenter tunnel, screen sharing and source-code tabs. Start probe traffic. Confirm the healthy revision ID and test the rollback command. Check no unintended fault remains enabled. Reserve new idempotency keys. Preserve the test database until the session ends. Keep the recording on the presentation laptop.

If a live component cannot be recovered within two minutes, switch to the recording and explain the observed issue. Do not provision infrastructure or troubleshoot IAM in front of the audience.

## Demo 1, 20 minutes

| Step | Action | What to explain |
|---|---|---|
| 1 | Show the four-function app and working legacy route | Same business outcome, explicit existing dependency |
| 2 | Inspect legacy MVC controller and shared booking library | Separate runtime concerns from business rules |
| 3 | Route search to .NET 10, leaving booking on IIS | Incremental migration and reversible routing |
| 4 | Route booking to .NET 10 with the same SQL database | Data correctness during coexistence |
| 5 | Submit a booking, retry exactly the same key | One booking and one seat allocation |
| 6 | Show versioned image and deployment configuration | Repeatable packaging and bounded changes |

If real Windows execution did not pass the rehearsal, use the explicitly labelled sync-behavior simulation. Do not describe it as a completed .NET Framework runtime migration.

## Demo 2A, dependency failure, 8 minutes

1. Show healthy client probe, application status and notification count.
2. Enable the mock failure with `python3 ops/lab.py fault-on`.
3. Create two modern reservations. Both should commit successfully.
4. Invoke the dispatcher. Pending events should remain durable after HTTP failures.
5. Disable the fault. Wait for the retry eligibility delay, then invoke dispatch again.
6. Verify pending count reaches zero and repeated dispatch does not increase unique notifications.

Explain the accepted trade-off: the booking is confirmed while notification delivery is delayed. A failed payment could not be handled the same way without preserving an explicit pending/payment state.

## Demo 2B, bad release, 12 minutes

1. Record healthy baseline and identify the stable revision.
2. Send 10% of Cloud Run traffic to the prepared faulty revision.
3. Compare healthy liveness with failing search requests.
4. Show the client failure ratio and accelerated lab alert. Explain sample size and burn rate.
5. Name the incident owner and choose rollback as mitigation.
6. Restore 100% traffic to the verified stable revision.
7. Confirm new requests recover, then wait for the rolling dashboard window to clear.
8. Record detection and recovery durations. These are observations from the run, not promised recovery times.

For a local fallback, start a second faulty app process and direct the probe to it, then back to the healthy process. Explicitly call this a fault/recovery simulation rather than a cloud canary deployment.

## Questions for the audience

Which user journey matters most? Which dependency can fail without blocking it? Who can authorize rollback? How would we know a database restore worked? Which repeated manual step should we automate first?
