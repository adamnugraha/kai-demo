# Lab SIT and UAT checklist

Environment / image digest / database / tester / date:

| Check | Expected outcome | Evidence / result |
|---|---|---|
| Search | Correct synthetic train list | |
| Reservation | Durable booking and matching seat decrement | |
| Same key retry | Same booking, no extra seat use | |
| Changed payload with same key | Conflict | |
| Concurrent seat requests | No overselling | |
| Notification outage | Modern booking remains available | |
| App restart | Pending events remain durable | |
| Dependency recovery | Backlog drains on dispatcher execution | |
| Duplicate event | No extra notification | |
| Legacy Windows endpoint | Runs under IIS with SQL Server | |
| Migration stages | Correct route reaches intended runtime | |
| Bad revision | User metric fails despite healthy liveness | |
| Rollback | New requests recover on stable revision | |
| Missing telemetry | Unknown/unavailable monitoring is visible | |
| Access | Unauthorized requests rejected | |
| Cleanup | Only lab resources removed | |

Approval / outstanding blockers / fallback decision:
