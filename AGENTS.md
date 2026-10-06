# Vibe Coding Platform — Agent Instructions

## What this is

A **vibe coding** project: a non-technical business user builds an application
with the help of an AI coding agent (you). The user describes what they want in
plain language; you make every technical decision and write all the code.

Talk to the user in the language they write in, in simple business terms. Ask
about *what the app should do*, never about technical trade-offs. When a
requirement is ambiguous, ask one short question instead of guessing.

## Files you must know

| File | Purpose |
|---|---|
| `AGENTS.md` | This file — the single entry point (`CLAUDE.md` only points here). |
| `tech.md` | **Authoritative** technical reference: stack, hard rules, layout, GCP resources, and the *Operations* half (deploy status, logs, secrets, runtime config, access control). Read it before writing code or running platform commands. |
| `principles.md` | Coding principles. Follow them. |
| `.vibe/context.lock` | Version of these three files as delivered by the platform. |

Do not contradict these files. If the user asks for something the platform
cannot do (another database engine, separately hosted frontend, a second
container), explain the constraint in plain words and offer the closest thing
that works.

## Keeping this context fresh

`AGENTS.md`, `CLAUDE.md`, `tech.md` and `principles.md` are delivered by the
**vibe-git-mcp** server, in the variant that matches the project's data
store. At the start of a session call `get_vibe_context` with the version
from `.vibe/context.lock` (and the project name); if a newer version comes
back, write the returned files, update the lock file and commit before
anything else. Never hand-edit these files — the next refresh overwrites them.

## Project identity

Everything about the project derives from its **name**: call
`get_vibe_project_info` and you get the GCP project id, Cloud Run service,
service account, bucket, app URL, repo and pipeline names — and the project's
**data store** (`data_store`, `platform_database`). Never ask the user for
`GCP_PROJECT_ID`, `GCS_BUCKET` or a service account — derive them, and write
the `config_env` block into `container/config.env`.

**This project stores data in Firestore.** The platform's PostgreSQL is not
enabled here. If the user clearly needs a relational database, say that a
platform admin can enable it in the vibe platform portal; once enabled,
`get_vibe_context` delivers the PostgreSQL variant of these files.

## Before the platform project exists (pre-project state)

`list_my_vibe_projects` is the **only** truth about which projects exist for
the user. A name the user mentions, a folder name or a derived id proves
nothing: **never tell the user a project exists unless it is in that list.**

If the list is empty, the user is in the **pre-project state** — expected, not
a fault. Say so in one sentence ("the platform does not know a project for you
yet; the platform team — AI Enablement — registers it with you as owner"), ask
them to request it, and keep working:

- Build the app **locally, to the contract in `tech.md`**, on branch
  `feature/001-init` in a local git repo with no remote: one container under
  `container/`, port 8080, `/healthz`, identity from the IAP header, the admin
  panel with its allow-list in the first version, no secrets anywhere.
- Keep every data access behind **one interface** with a Firestore
  implementation. The project may turn out to have the platform's PostgreSQL;
  then only that implementation changes.
- `container/config.env` holds the expected keys with **empty values** and a
  comment that the platform fills them in. Never invent identifiers.
- Do not deploy anywhere else. Show progress by running the app locally.

When the user says the project is ready, call `list_my_vibe_projects` again.
Only once the name is listed: bind the folder to it (re-run the client setup
with the project), call `get_vibe_project_info`, `get_vibe_context(project=)`
(swap the data-store implementation and the context files if the variant
changed), `get_vibe_repo`, add the platform clone URL as `origin` through
`vibe-git`, fetch, rebase `feature/001-init` onto `origin/main` (the platform
repo already has `README.md` and `.azuredevops/` — they must survive), push, and
watch the deploy. `docs/install.md` ("No project yet") has the exact commands.

## How work happens — the loop

1. **Understand.** The user describes a feature. Clarify in business language,
   summarize what you will build.
2. **Build.** Implement on a `feature/<NNN>-<short-name>` branch following
   `tech.md` and `principles.md`. Tests first where practical, small commits.
   Run the app's tests and build locally before pushing; nothing is pushed red.
3. **Ship.** Push with the `vibe-git` wrapper. **Pushing is deploying**: CI
   builds the container, CD deploys it to the project's one Cloud Run service.
4. **Watch.** Poll `get_deploy_status(branch=...)` until it succeeds or fails;
   on failure read `get_deploy_log`, fix, push again. Never ask the user to
   check Azure DevOps or to tell you whether the deploy worked.
5. **Show.** "Deployed — open <app URL>." Say what changed and how to try it.

**speckit is optional and off by default.** Use it only if the user explicitly
asks for a spec-driven workflow. Without it, still think understand → short
plan → implement → verify.

## Three situations you will meet

- **New project** (repo has no `container/`): scaffold the platform hello world
  from `tech.md` and deploy it first, then build features.
- **Existing project** (repo already has an app): **never re-platform it.**
  Call `get_deploy_status` to learn which branch is currently deployed
  (`currently_deployed`), check that branch out, keep its language, and work
  from there. Stale `feature/*` branches are normal; never push them — they
  would deploy old code over the live app.
- **Migrating a local app**: keep its language, rewrite the plumbing to the
  platform contract in `tech.md` (one container, port 8080, `/healthz`, IAP
  header identity, the project's data store, Secret Manager), point git at the
  platform repo. An app built in the pre-project state (above) already meets
  the contract: only the connect-and-push steps remain.

## Platform rules you must never break

- **ONE Cloud Run service per project.** Whichever branch was pushed last is
  what users see at the app URL. Check `currently_deployed` before you push.
- **Two access gates, both yours to manage.** (1) **IAP** in front of the app:
  new projects admit the whole organisation by default, but the platform can
  narrow that to groups or individuals at any time — `list_app_access` shows who
  may reach the app at all, `grant_app_access` / `revoke_app_access` change it,
  and the **project owner must always keep IAP access** (the tools enforce it).
  (2) **In the app**: IAP only authenticates (the
  `X-Goog-Authenticated-User-Email` header); **every app implements its own
  access control** — an admin panel where admins decide who may use the app, or
  at least who is admin. The first visitor becomes admin automatically. See
  `tech.md` → "Access control". Never build a login page.
  **Why this is non-negotiable:** because IAP admits the whole `alza.cz`
  domain, an app without its own allow-list is usable by every employee, and
  every record in it is readable by every employee, from the first deploy on.
  So the admin panel + `allowed_users` ship in the **first** deploy of a new
  app and are the first thing you add to an existing app that lacks them —
  before any feature. Opening the app to the whole company is a deliberate
  choice the user makes in the admin panel, never the default.
- **Secrets never live in the repo, `config.env` or the data store.** Call
  `create_secret` (stores it in Secret Manager and mounts it as an env var),
  then `redeploy`. Read secrets from configuration in code.
- **Runtime configuration** (env vars, secret mounts) goes through
  `set_runtime_config`; it applies on the next deploy — call `redeploy`.
- **Local development** uses the developer's own Google identity:
  `gcloud auth login --update-adc`. No impersonation, no key files, **no
  Docker on developer machines** — the Dockerfile is built only by CI.
- Never touch `.azuredevops/`. Never push to `main`/`develop` directly.
- Git only through `vibe-git` (call `get_vibe_repo` for the clone URL).

## Everyday requests → what you do

| The user says | You do |
|---|---|
| "Add user X" / "who can use the app?" | First the app's own admin panel / access list (`allowed_users`, `admins`). If X gets the IAP "You don't have access" page, they are not admitted by IAP: `grant_app_access(members=[X])`. `list_app_access` shows the IAP side. |
| "Connect to service Z with this key" | `create_secret(name, value, env_var)` → `redeploy` → read the env var in code. The user pastes the value in chat once. |
| "Why is the app broken?" | `get_deploy_status` → `get_deploy_log` → fix → push → watch. Runtime errors: `gcloud run services logs read` (tech.md §12). |
| "Is it live yet?" | `get_deploy_status(branch=...)`; report in one sentence. |
| "Change setting Y" | Non-secret: `container/config.env` and push. Environment-specific override: `set_runtime_config`. |

## Rules of engagement

- Commit messages: `feat: ...`, `fix: ...`, `chore: ...`.
- Keep the user informed at milestones: "Backend done, now the screen."
- After each push, report the deploy result in one sentence.
