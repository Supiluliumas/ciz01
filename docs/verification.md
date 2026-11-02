# Verification evidence

Latest verification: 2026-10-08 (Europe/Prague).
Earlier implementation/audit evidence: 2026-10-06.
Application: ciz01 / AI Mailer.
Branch: feature/001-init. Private GitHub backup authorized by the owner;
cloud deployment and live acceptance remain pending.

## Executed checks

- `dotnet test container/backend.tests`: **54 passed**, 0 failed, 0 skipped.
  Covers anonymous health vs protected APIs, atomic memory bootstrap, role
  changes, own request identity, duplicates, exact corporate domains, admin-only
  reviews, approval/rejection, concurrent decisions, revocation/reapplication,
  in-store revoked-admin checks, final-admin protection, individual allow-list and rejection of organisation-wide access,
  input limits, trusted proxy HTTPS restoration and origin rejection, Unicode
  boundaries, cross-origin/request-header rejection, duplicate/concurrent
  dispatch, uncertain timeout handling, Header Auth acknowledgement contracts,
  structured Vertex response and blocked output, bounded read-only data calls,
  unknown API authentication/access, and production configuration failure without fallback.
- `npm --prefix container/frontend run build`: TypeScript and Vite production
  build passed. Dependencies pinned and frontend package-lock committed.
- `npm --prefix container/frontend run test:e2e`: **17 passed** in the full
  verification run against its isolated published local app. Draft/edit/review/
  simulated send, admin operations, bounded sample table, desktop and mobile
  layouts, unavailable AI, uncertain send, denied user, empty table, failed data
  and admin-load retry, maximum Unicode body over actual HTTP, applicant pending/
  rejected/error states, real HTTP admin approval/rejection/revocation, mobile
  request layout and refresh failures without stale review controls.
- `node --test container/tools/n8n-workflow.test.mjs`: **9 passed**.
  Validates the actual template Code node, invalid inputs, Header Auth, fixed
  sender, response ordering, and execution-body retention settings.
- `python3.12 -m unittest discover -s container/tools`: **5 passed**.
  Configuration readiness, environment overrides, secret redaction, and invalid
  webhook/Header Auth configuration.
- `bash container/verify.sh`: complete local build/test/publish/browser flow
  passed using a separately owned server on 8099, which was then shut down.
- Strict readiness check correctly exited **2** for missing cloud configuration;
  missing external dependencies are not reported as deployment readiness.
- `dotnet publish container/backend -c Release`: succeeded.
- Public `/healthz` returned HTTP 200 with `{ "status": "ok" }`.
- Screenshots visually inspected: [desktop](screenshots/desktop.png) and
  [mobile](screenshots/mobile.png).
- n8n workflow JSON parsed and connections inspected; actual n8n import and
  execution are unverified external acceptance, not a passed integration test.
- Platform-provided context files are unchanged after fetch.

Production Google/n8n contract tests use fake HTTP responses, not live services.
Access tests use the memory store, not a live Firestore database. The real
Firestore implementation compiles but transactions still need cloud acceptance.
No emails were sent during these tests.

## Academy requirements

| Requirement | Implementation | Actual evidence / remaining work |
| --- | --- | --- |
| Own GCP project | Connect via Vibe MCP | Owner reports project approved; final assigned name/platform verification pending |
| Admin panel | /admin, access APIs, transactional stores | Backend and browser tests passed locally; cloud store unverified |
| Email through n8n | Review dialog, Header Auth client, workflow template | Client contracts and simulation passed; configure n8n and verify real receipt |
| Vertex AI | ADC REST structured generation | Contract tests passed; approved model/project IAM and real response pending |
| Secret Manager | N8N_AUTH_TOKEN runtime mount | App accepts server-side secret; create_secret and actual mount pending |
| BigQuery test read | Configured table metadata + tabledata.list maxResults=20 | Contract and preview tests passed; academy table/IAM/live read pending |
| Spec Kit | Installed CLI, constitution, spec, plan, tasks, implementation | Workflow initialized and artifacts committed before implementation |

## Exact external inputs needed

1. Assigned short Vibe project name, registered with the user as owner/admin.
   Owner-approved short name: ciz01. Actual IDs must come from platform MCP.
2. Production n8n webhook URL and matching Header Auth credential stored using
   Secret Manager, plus owner Outlook credential in the n8n workflow.
3. Academy BigQuery project/dataset/table reference and appropriate read grants.
4. Approved Vertex model available in europe-west4 and invocation permissions.

Until these are verified, this is a locally reviewed implementation ready for
connection, not a completed cloud assignment or proof of certification.

## Convergence review

Spec Kit Converge identified four actionable local gaps: forwarded HTTPS,
input-boundary verification, UI failure/uncertain states, and repeatable
verification/configuration readiness. T019–T022 were appended before fixes and
are now complete. T017–T018 remain external, unchecked cloud acceptance tasks.
Platform context 4863456b58c9 was checked again and is current.

## Canonical install audit

Full audit against root install.md completed on 2026-10-06. See
[install-audit.md](install-audit.md) for all setup sections, platform status,
source precedence, authorized exceptions, and pending live verification.
The complete local suite passed: 41 backend + 12 browser + 9 workflow + 5
readiness = 67 tests. No remote or cloud deployment was performed in this audit.

## Access approval verification — 2026-10-08

Spec Kit 002-access-requests spec/plan/research/data/contracts/tasks were created
and committed before implementation. New request tests failed first; full final
`bash container/verify.sh` passed: **54 backend + 17 browser/HTTP + 9 workflow +
5 readiness = 85 tests**. Frontend build and Release publish passed. New mobile
request screenshot visually inspected. Read-only design/code review covered
transaction authorization and all-reads-before-writes. A stale admin refresh
notice bug was corrected and has a browser regression test.

The existing mailer scenarios still pass. Local services remain simulations;
request lifecycle tests exercise memory, not live Firestore. No emails sent.
Mode everyone no longer grants access even if present in old Firestore settings.
The first-administrator race is handled operationally by owner-only IAP during
initialization, preserving platform bootstrap; verify this before first deploy.

Google sign-in has expired: get_vibe_context returned RefreshError, so no current
context freshness or project-list verification is claimed for this session.
The gcloud auth login --update-adc browser flow was launched for human completion.
Project approval is owner-reported; actual assigned name and metadata, IAP,
Firestore/store variant and all real integrations remain cloud acceptance.

## Platform recheck after supplied project name — 2026-10-08

Owner supplied `prj-gcp-l-plat-gencode-ciz01`; short name ciz01. Browser sign-in
completed and ADC updated successfully. gcloud active account confirmed as
martin.cizkovsky@alza.cz, matching the owner's explicit account confirmation.
list_my_vibe_projects succeeded but listed only unrelated shub, so ciz01 is
not registered for this account or owner/admin access is missing. No project
binding, identifiers, repository connection, IAM change or deployment performed.
Generic get_vibe_context known_version=4863456b58c9 returned up_to_date=true.
The earlier RefreshError was resolved; AI Enablement must verify ciz01
registration/ownership before connecting according to install section 4.3/4.4.

## GitHub reviewer handoff — 2026-10-08

Private repository: https://github.com/Supiluliumas/ciz01. User explicitly
requested uploading the complete committed assignment for supervisor review;
this is an authorized exception to the platform-only remote default. Remote
is github; future Vibe origin remains separate. No Cloud Run/IAM change or live
integration acceptance is implied. Full local verification was rerun before
push. Historical Git blobs scanned for common credential/private-key patterns:
179 scanned, no likely matches (pattern scan, not proof for all secret formats).

## n8n aligned with the internal Alza n8n guide — 2026-11-02

Owner supplied the internal n8n guide. Workflow template now targets the DEV
instance n8n.dev.gcp.alza.cz, sends through the Microsoft Outlook node with the
admin-provided owner credential (SMTP removed), uses the unique path
ciz01-mailer, descriptive Czech names, and an Error Trigger that notifies the
owner (guide requires failure notification for published workflows). Validator
now also rejects `,`/`;` recipient lists. N8N_WEBHOOK_URL set to the production
path; it works only after the owner imports and publishes the workflow.
Full `bash container/verify.sh` passed: 54 backend + 17 browser/HTTP + 12
workflow + 5 readiness = 88 tests. Import into the real n8n, Outlook send and
Error Workflow setting are not yet verified; no n8n access from the agent.
Platform recheck on 2026-11-02 failed with an expired Google sign-in; the owner
reports ciz01 is still not listed.


## GCP project created, Vibe registration still missing — 2026-11-02

AI Enablement e-mail: GCP project prj-gcp-l-plat-gencode-ciz01 created (old 1.0
style access forms for GCP, Azure DevOps and a Basic licence attached; the 2.0
install guide needs no Azure DevOps access or licence). After sign-in renewal,
`gcloud projects describe l-plat-gencode-ciz01` returns ACTIVE with display name
prj-gcp-l-plat-gencode-ciz01. `list_my_vibe_projects` still lists only shub, so
ciz01 is not registered on the Vibe platform with the owner. config.env is left
empty: identifiers must come from get_vibe_project_info. A name-based search of
visible GCP projects found no evident academy BigQuery project.
