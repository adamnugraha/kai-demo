# GCP deployment runbook

Primary recommendation: Jakarta region `asia-southeast2`, Cloud Run plus SQL Server, VPS mock dependency. Deploy only into the chosen lab project. These files require a real account rehearsal; successful Terraform validation does not confirm IAM, quotas, network reachability, SQL editions or regional capacity.

## 1. Foundation and cost review

The optional `infra/gcp/foundation` creates a VPC, subnet, private service access, Cloud SQL for SQL Server 2022 Standard, a database, Artifact Registry, a runtime service account and empty secret resources. SQL Server is billable and may dominate lab cost. Obtain an account-specific estimate before apply. Budget alerts notify; they do not cap spending.

Use ZONAL SQL for a cheaper functional lab or set database_ha=true for the regional data-tier exercise. Single-zone SQL is a declared failure point. The application demo does not prove zone or region failover. No VPN, Windows VM, DNS, billing budget or automated shutdown is created here.

```bash
cd infra/gcp/foundation
cp terraform.tfvars.example terraform.tfvars
# Edit only project and region. Supply the SQL administrator password through
# your protected shell as TF_VAR_sql_admin_password, without printing it.
terraform init
terraform plan -out=foundation.tfplan
# Review cost, resource scope and plan before:
terraform apply foundation.tfplan
```

Protect local state and saved plans as secrets. Use a protected remote backend if multiple operators collaborate. SQL deletion protection defaults on. APIs stay enabled when the stack is destroyed.

## 2. Database identity and secrets

From an approved host with private SQL connectivity, create an application SQL login and database user. Grant the rights needed to initialize the schema, then reduce runtime rights to the table reads/writes. This is a database-admin bootstrap step and is intentionally not performed with the routine deployer identity. Cloud IAM and SQL login permissions are distinct.

Put the SQL connection string into Secret Manager `kai-lab-db`, and the same random lab token used by the VPS into `kai-lab-token`. Use a protected local file or console input. The connection string points to the private SQL endpoint, database `railbooking`, with encryption and appropriate certificate validation. Avoid placing administrator credentials in the long-lived runtime secret.

## 3. Build the image

From the repository root, in an authenticated terminal with Docker:

```bash
export GCP_PROJECT=YOUR-PROJECT
export GCP_REGION=asia-southeast2
export IMAGE="$GCP_REGION-docker.pkg.dev/$GCP_PROJECT/kai-workshop/railbooking:rehearsed"
gcloud auth configure-docker "$GCP_REGION-docker.pkg.dev"
docker build --platform linux/amd64 -t "$IMAGE" .
docker push "$IMAGE"
```

Record the immutable image digest after the build. If using Cloud Build instead, configure its build identity and repository permissions explicitly. The application deployer role list does not automatically grant Cloud Build access.

## 4. Initialize, then deploy

Edit `infra/gcp/terraform.tfvars` from its example. Use the real image, runtime identity, existing network/subnet and HTTPS notification URL. The secrets must already have versions.

```bash
cd infra/gcp
terraform init
# Intentional first-install ordering: create and run the initializer before readiness checks.
terraform plan -target=google_cloud_run_v2_job.init -out=init.tfplan
terraform apply init.tfplan
gcloud run jobs execute kai-railbooking-init --project "$GCP_PROJECT" --region "$GCP_REGION" --wait
terraform plan -out=application.tfplan
terraform apply application.tfplan
```

Reduce schema privileges after initialization. Jobs can be rerun to create missing tables, but they are not a schema migration/versioning system. Review future migrations separately.

The service remains IAM-authenticated. For the presenter browser, use an authenticated local proxy:

```bash
gcloud run services proxy kai-railbooking --project "$GCP_PROJECT" --region "$GCP_REGION" --port=8080
```

Open the local URL and enter the separate application lab token. For the VPS client probe, use an approved invoker identity, set GOOGLE_ID_TOKEN locally and refresh it before the demo. ID tokens expire. A service account or federation flow may be required for a stable audience-specific token. Do not change service access to public merely to bypass authentication troubleshooting.

## 5. Notifications and canary

The presenter can click Deliver pending notifications or run the dispatch job. It uses the same durable outbox and HTTP dependency. There is no managed message broker in this build.

```bash
gcloud run jobs execute kai-railbooking-dispatch --project "$GCP_PROJECT" --region "$GCP_REGION" --wait
```

Record the current healthy revision. Create a new revision with the same rehearsed image and FAULT_PERCENT=100, APP_VERSION=canary-bad, using `--no-traffic`. Keep all existing environment, networking and secret settings. Then inspect revisions and set STABLE_REVISION and CANARY_REVISION locally.

```bash
gcloud run services update kai-railbooking --project "$GCP_PROJECT" --region "$GCP_REGION" --update-env-vars FAULT_PERCENT=100,APP_VERSION=canary-bad --no-traffic
bash ops/gcp-rollout.sh canary
# Observe the external probe and then:
bash ops/gcp-rollout.sh rollback
```

Run the rollout script from the repository root. Re-check traffic after rollback. A canary at 10% that fails every request produces approximately 10% overall failures under a representative sample, about 100 times the allowed error rate for a 99.9% SLO. Short sample variation is expected.

Terraform ignores traffic changes to preserve operator-controlled canaries. Before a later apply, review environment changes and revision creation carefully. Rollback changes application traffic, not database contents. Delete the faulty revision only after traffic is healthy and the evidence is saved.

## Cleanup

Record evidence and optional backups. Destroy the application stack with a reviewed plan. For the foundation, deliberately disable SQL deletion protection and review backup requirements before destroying the database. Delete image versions and secret versions only within this lab. Check for remaining disks, IP addresses, logs, backups, registries and VM resources. Confirm costs in billing after deletion.

References: https://docs.cloud.google.com/run/docs/container-contract
https://docs.cloud.google.com/run/docs/rollouts-rollbacks-traffic-migration
https://docs.cloud.google.com/sql/docs/sqlserver/connect-run
