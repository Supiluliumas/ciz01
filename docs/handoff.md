# Handoff — ciz01 (state on 2026-11-03 07:20 CET)

For the next coding agent (Codex) taking over from Claude Code. The owner is
Martin Čížkovský (`martin.cizkovsky@alza.cz`), a non-technical business user:
talk to him in Czech, in plain business terms, and make the technical decisions
yourself.

## Read first

1. `AGENTS.md`, `tech.md`, `principles.md`: platform context **92e43f1cc513**
   (fetched for project ciz01 and committed). Never hand-edit; refresh with
   `get_vibe_context(project="ciz01", known_version=<.vibe/context.lock>)`.
2. Root `install.md` (the owner made it canonical; see `docs/assignment.md`).
3. `docs/assignment.md` (7 academy requirements), `docs/verification.md`
   (evidence log, newest at the bottom), `docs/ready-to-deploy.md`,
   `docs/access.md`, `docs/n8n.md`, `specs/001-ai-mailer`, `specs/002-access-requests`.

## What the app is

AI Academy certification app: the user describes an e-mail, Vertex AI drafts
it, the user edits/reviews and explicitly confirms, the backend posts it to an
n8n webhook (Header Auth) that sends it through Outlook from the owner's
mailbox. Plus an admin panel (first visitor = admin, access requests with
approval, last-admin protection) and a read-only BigQuery test-table preview
(max 20 rows). .NET 10 backend + Node frontend in one container, Firestore,
Spec Kit used (explicit assignment requirement). Local demo:
`bash container/run-local.sh`; full offline check: `bash container/verify.sh`
(last run 2026-11-02: 54 backend + 17 browser + 12 workflow + 5 readiness = 88
passing).

## Platform state (verified)

| Item | Value |
|---|---|
| Vibe project | `ciz01`, listed by `list_my_vibe_projects` since 2026-11-02 (owner registered) |
| GCP project | `l-plat-gencode-ciz01` (europe-west4), data store **Firestore** |
| Cloud Run / SA | `cr-gc1-l-plat-gencode-ciz01-ciz01app` / `sa-gcp-ciz01@l-plat-gencode-ciz01.iam.gserviceaccount.com` |
| App URL | https://ciz01.vibe.alza.cz (nothing deployed; `currently_deployed: null`) |
| Platform repo | `https://azdomcp.vibe.alza.cz/git/ciz01App`, default `main` (protected) |
| Last pipeline run | platform's own tag `v1.0.0` build failed only because the empty repo has no `container/` — not our problem |
| `container/config.env` | `GCP_PROJECT_ID`, `GCS_BUCKET`, `N8N_WEBHOOK_URL` filled; `VERTEX_MODEL`, `BIGQUERY_*` empty |

## Git state

- Branch `feature/001-init`, working tree clean, 7 commits ahead of the
  `github` remote. **No `origin` yet.**
- `github` = owner-authorized private backup (https://github.com/Supiluliumas/ciz01)
  for academy reviewers; it does not deploy. Push there only when the owner asks.
  After the rebase onto the platform repo (below) the backup branch diverges and
  needs a force push — ask the owner first.
- `vibe-git` is not on PATH: use `~/vibe/vibe-git-mcp/vibe-git`.

## Blockers

1. **Owner has no GCP permissions in `l-plat-gencode-ciz01`.** At 07:19 on
   2026-11-03 `testIamPermissions` returned only `resourcemanager.projects.get`;
   Firestore, Storage and Vertex (`gemini-2.5-flash`, `gemini-3-flash`,
   `gemini-3-flash-preview`, europe-west4 and global) all return 403. Vibe
   registration should grant them automatically; Jakub Jakeš (AI Enablement)
   found the cause and said it should work on 2026-11-03. Fallback: Jira form
   "Přístup do Google Cloudu" https://jira.alza.cz/servicedesk/customer/portal/9/create/368.
   Not needed for deploying (the app uses its own SA), needed for choosing the
   Vertex model and for local runs against real resources.
2. **n8n token secret.** Context 92e43f1cc513 forbids self-service third-party
   secrets (`tech.md` §14): no `create_secret`, no secret in `set_runtime_config`,
   do not take the value in chat. Assignment requirement 5 wants it in Secret
   Manager, so the **platform team** must create the secret and mount it as
   `N8N_AUTH_TOKEN` on the Cloud Run service. Ideally the owner adds the secret
   version himself (`gcloud secrets versions add`) once he has GCP access, so
   the value is never shared. Request not confirmed as sent.
3. **BigQuery test table** (project/dataset/table) must come from the academy;
   not found anywhere in the repo or by project name. Request not confirmed as sent.
4. **App refuses to start in cloud mode** while `VERTEX_MODEL`,
   `N8N_AUTH_TOKEN` or `BIGQUERY_*` are missing (`container/backend/AppSettings.cs`).
   A deploy today would crash at startup.

## Open owner decision (asked twice, unanswered)

Proposed: make n8n sending and the BigQuery preview **optional until
configured** (the app starts; those screens say "zatím nenastaveno"), then
deploy now with the admin panel, access requests and AI drafting. This matches
`tech.md` §14 ("build the rest without it"). Alternative: wait until all inputs
exist. Get his answer before changing startup validation or pushing.

## Next steps for the agent

1. Re-bind the MCP client to the project and restart:
   `VIBE_PROJECT=ciz01 bash ~/vibe/vibe-git-mcp/setup.sh codex`
   (`~/.codex/config.toml` currently has `VIBE_PROJECT = ""`). Then
   `get_vibe_context(project="ciz01", known_version=…)`; commit if newer.
2. Recheck owner permissions (testIamPermissions on the project, a Vertex
   `generateContent` probe). When granted: pick an available Gemini model in
   europe-west4 and set `VERTEX_MODEL` in `container/config.env`.
   zsh gotcha: write `${VAR}` in URLs — `$P:testIam…` / `$m:generateContent`
   are mangled by zsh modifiers.
3. If the owner approves the partial deploy: tests first, then optional
   integrations (status reported by the API, explicit UI state, readiness
   check and docs updated, Spec Kit spec/plan/tasks updated). Keep
   fail-fast for everything else (`GCP_PROJECT_ID`, malformed values).
4. Before the first push: restrict IAP to the owner only (`list_app_access`,
   then narrow; the owner always keeps access) — `docs/access.md` "Initial
   deployment and IAP gates". The owner opens the URL first and becomes admin;
   only then admit colleagues.
5. Connect and deploy (install.md 4.4 step 6): `vibe-git remote add origin
   https://azdomcp.vibe.alza.cz/git/ciz01App`, `vibe-git fetch origin`,
   `git rebase origin/main` (keep the platform's `.azuredevops/` and
   `.gitignore`), `bash container/verify.sh`, `vibe-git push -u origin
   feature/001-init`, poll `get_deploy_status(branch="feature/001-init")`,
   read `get_deploy_log` on failure. Report in one Czech sentence.
6. When the secret mount, BigQuery reference and n8n workflow exist: fill
   config, `redeploy`, run the live acceptance in `docs/ready-to-deploy.md`
   ("Demonstration for academy review") and record real evidence in
   `docs/verification.md`. Never claim delivery from an HTTP 200 alone.

## Owner's open steps (Martin)

1. **GCP permissions** — check with Jakub Jakeš that his fix is applied; if not
   by the afternoon, file the Jira form above.
2. **Decide the first deploy** (section "Open owner decision").
3. **n8n** (invite received 2026-11-02 from n8nadmin@alza.cz):
   a. sign up via the personal invite link (do not forward it);
   b. request Microsoft/Outlook services via the form linked in the invite;
   c. then the six steps in `docs/n8n.md`: import `docs/n8n-workflow.json`,
      Header Auth credential `X-N8N-Token` with a long random value (keep it
      private), select the Outlook credential in both Outlook nodes, set the
      workflow as its own Error Workflow, test to his own address, Publish.
4. **Secret mount** — ask AI Enablement to create the Secret Manager secret
   for the n8n token in `l-plat-gencode-ciz01`, mount it as `N8N_AUTH_TOKEN`
   on `cr-gc1-l-plat-gencode-ciz01-ciz01app`, and allow him to add the version.
5. **BigQuery** — ask the academy for the exact test table
   (project/dataset/table) and read access for the app's service account.
6. **Reviewers** — invite academy reviewers to the private GitHub repository.
7. When Google sign-in expires: `gcloud auth login --update-adc` (browser).
