# Presenter notes

KAI technical meeting, 30 October 2026. Two-hour session.

## Slide 1: Achieving 99.9% availability

Opening, 2 minutes. Introduce one fictional rail reservation application. The objective is a repeatable method for improving reliability. 99.9% is a teaching target, not a claim about KAI or a guarantee from this lab.

## Slide 2: The outcomes for today

3 minutes. Ask who owns application changes and who receives incident calls. These responsibilities meet at the same customer journey.

## Slide 3: A reservation is a customer outcome

3 minutes. Synthetic application only. A working server does not establish that reservations work. Payment handling is outside this lab.

## Slide 4: The meaning of the nines

2 minutes. Calculation uses 43,200 minutes in 30 days. Explain the difference between time-based and request-based measurements. Source: https://sre.google/workbook/implementing-slos/

## Slide 5: Two sessions, one application

Transition into session one. Detailed timing is in the presenter runbook. Keep each live demo to 20 minutes.

## Slide 6: Application modernization

Begin the modernization discussion. Preserve the useful business behavior while changing the runtime and operational weaknesses.

## Slide 7: The starting application

3 minutes. This is a deliberately small example. It is not an assessment of KAI systems. Both versions retain transaction and idempotency protections so the demonstration can isolate runtime and dependency behavior.

## Slide 8: Modernization decisions

3 minutes. .NET 10 is an LTS release supported through November 2028. Moving to Linux requires compatible libraries. Web Forms, COM, Windows authentication and other platform dependencies need assessment. Source: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core

## Slide 9: Incremental migration

3 minutes. Demonstrate explicit routing. This lab has a small API adapter. For general application routing, Microsoft documents YARP with incremental migration. Existing authentication, cookies and shared state require explicit design. Source: https://learn.microsoft.com/en-us/aspnet/core/migration/fx-to-core/start?view=aspnetcore-10.0

## Slide 10: Booking correctness

3 minutes. Walk through the shared library. An HTTP timeout can happen after a commit. Idempotency lets the caller safely discover the original outcome. Database constraints and transactional updates protect inventory.

## Slide 11: The hybrid lab

2 minutes. Windows can be an existing machine or a disposable VM. Linux VPS resources cannot run classic .NET Framework natively. The local SQLite fallback is a single-instance rehearsal environment.

## Slide 12: Cloud deployment choices

2 minutes. Show one cloud live. Reuse the container on the other cloud after the main environment works. Similar roles do not mean identical deployment or failure behavior.

## Slide 13: Demo 1: the migration

20 minutes, including questions. Open the browser, legacy controller, shared transaction code and deployment configuration. Show X-Backend response headers. Use the actual Windows path only after it passes rehearsal. The alternative sync-mode demonstration must be described as a behavior simulation.

## Slide 14: What the architecture now supports

5 minutes including discussion, then take the five-minute break. Containers and managed services do not eliminate operational responsibility.

## Slide 15: Site reliability engineering

Resume after the break. Use the same application and customer journey. Introduce indicators, objectives, error budgets and response decisions.

## Slide 16: Indicator, objective and agreement

3 minutes. An SLO needs a measurement boundary and owner. A component SLA does not automatically become an end-to-end application SLA. Source: https://sre.google/workbook/implementing-slos/

## Slide 17: The measurement boundary

3 minutes. The provided dashboard measures the synthetic search probe. App counters describe the instance that served a request. Do not scrape one load-balanced endpoint and assume it aggregates every Cloud Run instance.

## Slide 18: The error budget

3 minutes. Calculation: 1,000,000 times 0.001. Use the budget to prioritize reliability work and decide release risk. Reaching the budget does not mean stopping a change that would restore the service. Source: https://sre.google/workbook/implementing-slos/

## Slide 19: Budget burn during a bad release

2 minutes. A 10% canary that fails every request produces approximately 10% overall failure under a representative sample. This is arithmetic, not measured lab evidence. Traffic mix and small samples affect the observed result. Source: https://sre.google/workbook/alerting-on-slos/

## Slide 20: The operating view

3 minutes. The supplied implementation includes client metrics and structured logs. Distributed tracing is a useful next addition, but this package does not claim to contain a full tracing stack.

## Slide 21: Alerts with a useful response

3 minutes. The short alert exists so the audience can observe it. It is not a production paging recommendation. External alert delivery is not configured in this package. Source: https://sre.google/workbook/alerting-on-slos/

## Slide 22: Demo 2A: dependency failure

8 minutes. The presenter invokes the outbox dispatcher explicitly. Explain that a periodic or continuous worker is needed for automatic production delivery. Do not apply this behavior blindly to mandatory payment confirmation.

## Slide 23: Demo 2B: a faulty release

12 minutes. Cloud Run supports traffic splitting and revision rollback. In-flight requests and rolling metric windows do not clear instantly. Traffic rollback cannot undo a database migration. Source: https://docs.cloud.google.com/run/docs/rollouts-rollbacks-traffic-migration

## Slide 24: Incident response ownership

2 minutes. In a small team, one person may hold several roles. The responsibilities still need explicit owners. Capture actual detection and recovery timings without presenting them as guarantees.

## Slide 25: Availability and recovery

2 minutes. RTO describes the recovery-time objective. RPO describes acceptable data loss. A backup success message is insufficient evidence of a working restore. One VPS, even with several containers, is one host failure domain.

## Slide 26: A first KAI reliability pilot

10 minutes. Use templates/07-pilot-charter.md. Ask participants to work in pairs. Avoid selecting the largest system as the first pilot.

## Slide 27: The next 30 days

Close the exercise by asking each team to nominate one owner and one first action. These are proposed steps, not commitments already made by KAI.

## Slide 28: Discussion

Reserve the final ten minutes for questions. Return to the assessment, SLO and incident worksheets. Discuss actual user impact and operational ownership.

## Slide 29: Appendix: access model

Reference if access questions arise. AWS recommends federation and temporary credentials. GCP Workload Identity Federation provides a keyless automation option. Sources: https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html ; https://docs.cloud.google.com/iam/docs/workload-identity-federation-with-deployment-pipelines

## Slide 30: Appendix: participant materials

The reusable files are in the workshop package. Reference sources are in docs/SOURCES.md. Adapt definitions and targets to each real service.

