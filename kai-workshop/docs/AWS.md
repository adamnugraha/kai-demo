# AWS alternative

The AWS file is an application stack, not a complete account foundation. Use it after the primary environment works. It creates an ALB with HTTPS, a Fargate cluster/service, task roles, logs and security groups. Supply an existing VPC with two public subnets in different AZs, a privately reachable SQL Server database, a certificate, the image and two secret ARNs.

1. Authenticate using an approved federated identity or short-lived role session.
2. Create a dedicated ECR repository. Build the same Linux AMD64 image, authenticate Docker to ECR, and push the rehearsed image.
3. Prepare SQL Server and the `railbooking` schema using the same instructions as the local SQL profile. An existing lab database avoids creating a second expensive database. A database on one VPS remains a failure point.
4. Store the SQL connection string and token as separate SecretString values in Secrets Manager. If using customer-managed KMS encryption, grant the execution role access to that exact key.
5. Review `infra/aws/ecs-app.yaml` and create a CloudFormation change set in the lab account. The template creates IAM roles and therefore requires CAPABILITY_IAM. Review before execution.
6. Permit the returned task security group to reach the SQL endpoint on 1433. Keep database ingress private. Configure the hostname covered by the ACM certificate to resolve to the ALB.
7. Run the same booking, outbox and failure tests. Use ECS service events and CloudWatch logs when a task fails readiness.

The tasks use public IPs for outbound image pulls and HTTPS without a NAT gateway. Security groups admit application traffic only from the ALB. A production design may use private subnets and VPC endpoints or NAT. The choice affects cost and is not hidden by this template.

Desired count two provides application replicas. It does not prove database HA, AZ failure survival or end-to-end availability. The circuit breaker covers deployment failure detection; a business-logic bug that passes health checks still needs user-outcome monitoring and an operator rollback.

For this first AWS version, trigger the dispatcher through the protected operations endpoint or create an explicit scheduled task after rehearsal. SQS, cross-cloud replication and automatic cross-cloud failover are not implemented.

Delete only the named lab CloudFormation stack after reviewing retained data. The existing database, secrets, image repository and certificate remain separately owned and need an explicit cleanup decision.
