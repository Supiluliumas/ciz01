# Handoff — ciz01 (state on 2026-11-03, post first deploy)

For the next coding agent taking over. The owner is Martin Čížkovský
(`martin.cizkovsky@alza.cz`), a non-technical business user: talk to him in
Czech, in plain business terms, and make the technical decisions yourself.

## Read first

1. `AGENTS.md`, `tech.md`, `principles.md`: platform context **92e43f1cc513**
   (current for ciz01, verified 2026-11-03). Never hand-edit; refresh with
   `get_vibe_context(project="ciz01", known_version=<.vibe/context.lock>)`.
2. `docs/verification.md` (evidence, newest at bottom), `docs/access.md`,
   `docs/ready-to-deploy.md`, `docs/n8n.md`, `specs/001-ai-mailer`,
   `specs/002-access-requests`.

## What the app is

AI Academy certification app: Vertex AI drafts an email, the user reviews and
explicitly confirms, the backend posts it to an n8n webhook (Header Auth,
Outlook from the owner's mailbox). Admin panel + access requests, read-only
BigQuery test-table preview. .NET 10 + Node frontend, one container, Firestore.
Local demo `bash container/run-local.sh`; full check `bash container/verify.sh`
(98 tests passing 2026-11-03: 61 backend + 18 browser + 12 workflow + 7
readiness).

## Platform state (verified 2026-11-03)

| Item | Value |
|---|---|
| Vibe project | `ciz01` (listed for owner) |
| GCP project | `l-plat-gencode-ciz01` (europe-west4), data store **Firestore** |
| Cloud Run / SA | `cr-gc1-l-plat-gencode-ciz01-ciz01app` / `sa-gcp-ciz01@l-plat-gencode-ciz01.iam.gserviceaccount.com` |
| App URL | https://ciz01.vibe.alza.cz — **DEPLOYED** (CD run 2179822, commit `b4c1970`, `feature/001-init`) |
| Platform repo | `https://azdomcp.vibe.alza.cz/git/ciz01App` as `origin`; `main`/`develop` protected; only `feature/*` branches deploy |
| `container/config.env` | all platform ids + `VERTEX_MODEL=gemini-2.5-flash`, `N8N_WEBHOOK_URL`, `BOOTSTRAP_ADMIN_EMAIL=martin.cizkovsky@alza.cz`; `BIGQUERY_*` empty, `N8N_AUTH_TOKEN` waits for secret mount |

Deployed variant: n8n sending and the BigQuery preview are **optional until
configured** — `/api/status` reports `configured`/`partial`/`not-configured`,
those screens say "nenastaveno", and everything else works (admin panel,
access requests, AI drafting if the SA can reach Vertex — not yet proven).

## Git state

- `origin` = platform repo (clone via `~/vibe/vibe-git-mcp/vibe-git`; plain git
  cannot auth). `feature/001-init` was rebased onto `origin/main` — platform's
  `.azuredevops/`, merged `.gitignore` and stub `README.md` handled; ours kept.
- `github` = owner-authorized private reviewer backup
  (https://github.com/Supiluliumas/ciz01). It holds the PRE-REBASE history
  (push through `edccc7f`); updating it to the deployed history needs a force
  push — the owner approved pushing current state but confirm again.
- MCP tools unavailable in this IDE session: call the same endpoint directly —
  `python3 /tmp/vibe_call.py` pattern (script recreates TokenMinter +
  X-Vibe-Project handshake); or register the shim for your own agent via
  `VIBE_PROJECT=ciz01 bash ~/vibe/vibe-git-mcp/setup.sh <host>`.

## Verified working vs not

- CI `2179810` + CD `2179822` succeeded; URL answers behind IAP (302 to Google).
- Owner opened the app yet? **Unverified** — only his visit creates the first
  admin (bootstrap restricted to his email) and proves Firestore writes work.
- Owner GCP permissions **still missing** at ~08:50 (only
  `resourcemanager.projects.get`; even `getIamPolicy` disappeared; Vertex and
  Cloud Run calls all 403/permission-denied). Jakeš said fixed at 08:06 — not
  effective. Fallback: Jira "Přístup do Google Cloudu"
  https://jira.alza.cz/servicedesk/customer/portal/9/create/368.
- `list_app_access` cannot manage IAP: platform SA lacks `roles/browser` in
  the project — report to AI Enablement. Meanwhile IAP admits the whole org;
  `BOOTSTRAP_ADMIN_EMAIL` covers the first-admin risk, the allow-list the rest.
- Runtime config/secret mount status unknown (`get_runtime_config` not called).

## Next steps for the agent

1. After the owner's first visit: verify he is admin, let him approve himself
   through the request flow if needed, then colleagues can request access.
2. When AI Enablement mounts `N8N_AUTH_TOKEN` (secret created by platform team,
   version added by the owner himself) and the academy supplies the BigQuery
   table + grants the app SA read access: fill `BIGQUERY_*` in config.env,
   push, confirm `/api/status` reports `configured`, run the live acceptance
   in `docs/ready-to-deploy.md` and record evidence in `docs/verification.md`.
3. If drafting fails at runtime (Vertex SA perms or model name), read Cloud Run
   logs once owner perms exist (`gcloud run services logs read`, tech.md §12)
   and adjust `VERTEX_MODEL`/`VERTEX_LOCATION`.
4. Recheck `list_app_access` periodically; when the platform can manage IAP,
   narrow admission to the owner + approved colleagues per docs/access.md.
5. On expired sign-in: `gcloud auth login --update-adc` (browser).

## Owner's open steps (Martin)

1. **Open https://ciz01.vibe.alza.cz** — first visit makes you administrator.
2. **GCP permissions** — Jakeš's fix still does not work; file the Jira form
   if not resolved today.
3. **n8n**: sign up via the invite link, request Outlook services via the form,
   then the six steps in `docs/n8n.md` (import `docs/n8n-workflow.json`,
   Header Auth `X-N8N-Token`, Outlook credentials on both nodes, Error
   Workflow, test to your address, Publish).
4. **Secret mount** — ask AI Enablement to create the n8n-token secret in
   `l-plat-gencode-ciz01`, mount it as `N8N_AUTH_TOKEN` on the Cloud Run
   service, and let you add the version yourself.
5. **BigQuery** — ask the academy for the exact test table and read access
   for `sa-gcp-ciz01@l-plat-gencode-ciz01.iam.gserviceaccount.com`.
6. **Reviewers** — invite academy reviewers to the private GitHub repo.
