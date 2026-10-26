# Real .NET Framework migration path

The legacy sample is ASP.NET MVC 5 on .NET Framework 4.8. It compiles as a Windows web application and must run under Windows/IIS. A Linux VPS cannot host this runtime natively. Build success on Linux using reference assemblies is not a Windows execution test.

Use an existing disposable Windows machine if available. Otherwise create a temporary Windows Server VM only after reviewing licensing and cost. Keep SQL connectivity private. For a GCP-hosted Windows VM, use the same VPC as the SQL database and restrict RDP to your approved access route. Do not expose RDP to the world.

1. Install .NET Framework 4.8, the .NET SDK used to build this project, IIS and ASP.NET 4.x features.
2. Initialize the shared SQL schema using the modern app's `--init-db` command with an authorized database identity.
3. Run `ops/Publish-Legacy.ps1` in an owner-controlled elevated PowerShell session. It creates a dedicated app pool and website. Default binding is loopback only.
4. Set LAB_TOKEN, DB_CONNECTION and NOTIFIER_URL in the deployed Web.config locally. Restrict file access to the administrator and application identity. Do not commit the edited config.
5. Verify `/api/trains` and a reservation directly, with the X-Lab-Token header. The notification endpoint must be reachable.
6. Configure the modern application with LEGACY_URL and MIGRATION_STAGE=legacy. All demonstrated API routes should reach IIS.
7. Change MIGRATION_STAGE=search. Search now runs on .NET 10 while booking still uses IIS.
8. Change MIGRATION_STAGE=modern. Search and booking both run on .NET 10, using the same SQL database.

Inspect X-Backend and X-App-Version headers during the migration. The shared .NET Standard library preserves booking rules while the web runtime changes. The adapter forwards only this lab's API routes. It is not a complete proxy for cookies, authentication, streaming or arbitrary legacy URLs. YARP and System.Web adapters are the expansion path for a real application assessment.

The baseline deliberately waits for the notification API after committing a booking. A notification outage can therefore produce an HTTP error even when the booking exists. Retrying the same idempotency key retrieves the original booking. The modern async version removes notification delivery from the booking response path. Both versions protect seat allocation and write the outbox.

If Windows is unavailable by Wednesday, use the modern app in NOTIFICATION_MODE=sync for the behavior comparison. Clearly label that as a simulation of legacy coupling, not an actual .NET Framework migration demonstration. Keep the source comparison and migration stages in the deck.

Reference: https://learn.microsoft.com/en-us/aspnet/core/migration/fx-to-core/start?view=aspnetcore-10.0
