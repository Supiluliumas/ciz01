# Validate approved corporate access

Run `bash container/verify.sh` from repo root. It runs local simulation only.
For interactive preview run `bash container/run-local.sh`; http://localhost:8088.

Automated backend scenarios bootstrap owner, request as another corporate
identity, deny all business/admin paths while pending, approve/reject as owner,
verify individual grant/revocation, deduplication and concurrent decisions.
Browser scenarios cover request/status page, pending/rejected states, admin
review and actual HTTP workflow with separate test identities.

After final project name: verify platform list, actual metadata/context/store,
connect preserving pipeline, temporarily owner-only IAP, deploy/watch exact
commit, initialize owner admin, then admit corporate identities to request page.
Confirm two real accounts: pending account denied features; same account approved
can use features; revocation denies subsequent calls. Record cloud evidence
separately; local tests do not prove IAP/Firestore IAM or deployment.
