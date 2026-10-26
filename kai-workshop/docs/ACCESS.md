# Access and ownership

An IAM role name, project ID or new VPS username does not by itself connect an assistant to your environment. This package can be deployed in your own Cloud Shell or terminal. Direct execution requires an explicitly connected, authenticated runner that can reach the target. There is no generic ChatGPT IAM principal to place in a trust policy.

Do not paste AWS access keys, GCP service-account JSON keys, SSH private keys, passwords, tokens or `.env` into chat. Non-secret project IDs, role ARNs, region names and machine specifications are sufficient for planning.

## Recommended execution route

For this deadline, use your existing signed-in Cloud Shell session and an owner-controlled terminal on the VPS. Run the supplied commands after reviewing the plan. A dedicated GitHub Actions runner with OIDC is a good repeatable option once a repository is selected. Restrict its trust to the exact repository and approved branch or protected environment.

## GCP identities

Use a dedicated, billed lab project. The account owner creates the initial project, enables services, provisions the network/database, creates identities and binds roles. The optional foundation Terraform performs these actions under that owner's approved identity. Keep these privileges separate from routine application deployment.

| Identity | Required scope |
|---|---|
| Application deployer | Cloud Run Developer on the lab project or specific resources, Artifact Registry Writer on the lab repository, Service Account User on the dedicated runtime account, Compute Network User on the chosen subnet for Direct VPC egress |
| Presenter/test runner | Cloud Run Invoker on the service and jobs it invokes |
| Runtime service account | Secret Accessor on `kai-lab-db` and `kai-lab-token` only |
| Infrastructure bootstrap owner | Approved permissions for API enablement, VPC/private service access, Cloud SQL, Artifact Registry, Secret Manager and IAM setup in the lab project |

The private TCP SQL connection uses a SQL login. It does not become authenticated to SQL Server just because the service account has a GCP role. If you choose Cloud SQL Auth Proxy instead, add the required Cloud SQL IAM permissions and connector configuration separately.

For CI, use Workload Identity Federation and short-lived credentials. Do not generate a permanent service-account key for this workshop. Secret contents should be added locally through Secret Manager or a protected terminal. Terraform foundation state contains the SQL administrator password even when the variable is marked sensitive. Protect that state and never put it in the ZIP or source repository.

## AWS identities

Use a dedicated sandbox account if possible, or isolated lab resources in your account. Human access should use IAM Identity Center/federation. Automation should assume a role with short-lived credentials. Restrict the trust policy to the actual authenticated runner; a role ARN alone is insufficient.

Separate the deployer, CloudFormation execution role, ECS execution role and ECS application task role. The supplied app template gives the ECS execution role access to the two named secrets and normal image/log operations. The task role has no AWS permissions because this version uses a SQL outbox and HTTP delivery. Add `kms:Decrypt` only for the exact customer-managed KMS key if your secrets require one.

The infrastructure execution role needs the reviewed EC2 networking, ELB, ECS, Logs and narrowly scoped IAM role-management actions required by the template. Do not use a generic AdministratorAccess role for the assistant. Resolve exact account/resource ARNs before producing a deployer policy.

## VPS

Use a dedicated account such as `kai-lab` with key-based login and access only to the lab directory. Prefer a separate VM if the VPS already hosts your MSP platform. The owner installs prerequisites. Rootless Docker is preferable where supported. Membership of the normal Docker group is effectively root access; it is not a harmless restricted permission.

Do not grant blanket passwordless sudo. If direct SSH execution becomes available, share only a newly generated public key for installation, never a private key. This package does not install an account, open SSH, or change your firewall.

Bind application, Grafana and Prometheus ports to loopback. Example owner-operated tunnel:

```bash
ssh -L 8080:127.0.0.1:8080 -L 8082:127.0.0.1:8082 -L 3000:127.0.0.1:3000 -L 9090:127.0.0.1:9090 kai-lab@YOUR-VPS
```

For the cloud-to-VPS notification demonstration, expose only `/notify` through authenticated HTTPS. `compose.https.yaml` provides optional Caddy configuration after you supply a domain and allow the required web ports. `/admin/fault` remains accessible through the SSH tunnel. Never expose SQL Server directly to the public internet.

## Details required before deployment

Primary cloud; lab account/project ID; region; maximum total lab spend and shutdown date; VPS OS, CPU, RAM, free disk and CPU architecture; whether the server hosts other workloads; Windows host availability; usable DNS name; presenter IP/CIDR; repository choice; exact meeting start time.

References: https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html
https://docs.cloud.google.com/iam/docs/workload-identity-federation-with-deployment-pipelines
https://docs.cloud.google.com/run/docs/deploying
https://docs.cloud.google.com/run/docs/configuring/services/service-identity
