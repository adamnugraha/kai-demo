#!/usr/bin/env bash
set -euo pipefail
: "${GCP_PROJECT:?Set GCP_PROJECT}" "${GCP_REGION:=asia-southeast2}" "${STABLE_REVISION:?Set the verified healthy revision}" "${CANARY_REVISION:?Set the faulty revision}"
case "${1:-}" in
 canary) gcloud run services update-traffic kai-railbooking --project "$GCP_PROJECT" --region "$GCP_REGION" --to-revisions "$STABLE_REVISION=90,$CANARY_REVISION=10" ;;
 rollback) gcloud run services update-traffic kai-railbooking --project "$GCP_PROJECT" --region "$GCP_REGION" --to-revisions "$STABLE_REVISION=100" ;;
 *) echo 'Usage: bash ops/gcp-rollout.sh canary|rollback';exit 1 ;;
esac
