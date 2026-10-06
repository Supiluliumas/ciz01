# Verification evidence

Date: 2026-10-06 (Europe/Prague).
Application: cizkovsky_ai_academy_ukol / AI Mailer.
Branch: feature/001-init. Private GitHub backup authorized by the owner;
cloud deployment and live acceptance remain pending.

## Executed checks

- `dotnet test container/backend.tests`: **41 passed**, 0 failed, 0 skipped.
  Covers anonymous health vs protected APIs, atomic memory bootstrap, role
  changes, final-admin protection, default allow-list and explicit opening,
  input limits, trusted proxy HTTPS restoration and origin rejection, Unicode
  boundaries, cross-origin/request-header rejection, duplicate/concurrent
  dispatch, uncertain timeout handling, Header Auth acknowledgement contracts,
  structured Vertex response and blocked output, bounded read-only data calls,
  unknown API authentication/access, and production configuration failure without fallback.
- `npm --prefix container/frontend run build`: TypeScript and Vite production
  build passed. Dependencies pinned and frontend package-lock committed.
- `npm --prefix container/frontend run test:e2e`: **12 passed** in the full
  verification run against its isolated published local app. Draft/edit/review/
  simulated send, admin operations, bounded sample table, desktop and mobile
  layouts, unavailable AI, uncertain send, denied user, empty table, failed data
  and admin-load retry, and maximum Unicode body over actual HTTP.
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
| Own GCP project | Connect via Vibe MCP | Pending dedicated project; platform list still only contains shub |
| Admin panel | /admin, access APIs, transactional stores | Backend and browser tests passed locally; cloud store unverified |
| Email through n8n | Review dialog, Header Auth client, workflow template | Client contracts and simulation passed; configure n8n and verify real receipt |
| Vertex AI | ADC REST structured generation | Contract tests passed; approved model/project IAM and real response pending |
| Secret Manager | N8N_AUTH_TOKEN runtime mount | App accepts server-side secret; create_secret and actual mount pending |
| BigQuery test read | Configured table metadata + tabledata.list maxResults=20 | Contract and preview tests passed; academy table/IAM/live read pending |
| Spec Kit | Installed CLI, constitution, spec, plan, tasks, implementation | Workflow initialized and artifacts committed before implementation |

## Exact external inputs needed

1. Assigned short Vibe project name, registered with the user as owner/admin.
   Owner-approved short name: cizkovskyai. Actual IDs must come from platform MCP.
2. Production n8n webhook URL and matching Header Auth credential stored using
   Secret Manager, plus owner SMTP mailbox in the n8n workflow.
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
