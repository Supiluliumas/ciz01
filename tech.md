# Vibe Coding Platform — Technical Reference

Every vibe application runs in its own GCP project, provisioned by the
platform team, and is built and deployed by Azure DevOps pipelines. This file
is the technical contract for the repository. The first half says how to build
the app; the second half ("Operations") says how to run it — through the
**vibe-git-mcp** tools, so the AI agent never needs the Azure DevOps website.

## 1. Project identity — derived from the project name

Call **`get_vibe_project_info`**. It returns, for project `<name>`:

| Value | Pattern |
|---|---|
| GCP project id | `l-plat-gencode-<name>` |
| GCP project name | `prj-gcp-l-plat-gencode-<name>` |
| Region | `europe-west4` |
| Cloud Run service | `cr-gc1-l-plat-gencode-<name>-<name>app` |
| App service account | `sa-gcp-<name>@l-plat-gencode-<name>.iam.gserviceaccount.com` |
| Storage bucket | `bkt-gcp-l-plat-gencode-<name>-<name>` |
| Firestore | `(default)` database, `europe-west4` — **this project's data store** |
| **Public URL** | **`https://<name>.vibe.alza.cz`** — behind IAP; never hand out the raw `*.run.app` address, it is not reachable |
| Git repository | `<name>App` in Azure DevOps project `platform-gencode` (clone via `get_vibe_repo`) |
| Pipelines | `<name>App-ci` (build) → `<name>App-cd` (deploy) |
| Runtime config | shared repo `gcp-configuration`, branch `lab`, folder `prj-gcp-l-plat-gencode-<name>/run/cr-gc1-l-plat-gencode-<name>-<name>app/` (managed via `set_runtime_config`) |

Write `GCP_PROJECT_ID` and `GCS_BUCKET` into `container/config.env` from that
output (`config_env`). Nothing else is project specific.

**No project yet?** If `list_my_vibe_projects` is empty, none of the values
above exist for this user — do not derive or invent them. Write the keys into
`container/config.env` with empty values under a comment
(`# filled in by the platform once the vibe project exists`) and build to the
rest of this document unchanged. Keep the `[Required]` validation of §6.1; to
run locally without a GCP project, put every data access behind one interface
and add an **in-memory implementation** selected only by the environment
variable `VIBE_LOCAL=1` (set in the shell, never in `config.env` or the
Dockerfile) — with it the GCP clients are not constructed and the settings
validation is skipped. Fill `config.env` from `get_vibe_project_info` the moment
the project appears in the list; the in-memory implementation stays useful for
tests.

## 2. Architecture

```
User ──> https://<name>.vibe.alza.cz ──> Load balancer (platform) ──> IAP ──> Cloud Run (this app, 1 container, port 8080)
                                                                                 ├── /healthz      health check (no auth)
                                                                                 ├── /api/*        backend endpoints
                                                                                 └── /*            React static files
                                                 Service account ──> Firestore / Cloud Storage / Vertex AI / Secret Manager (mounted as env vars)
```

- **IAP authenticates users** with their Alza account and forwards the identity
  in `X-Goog-Authenticated-User-Email`. New projects admit the whole
  organisation through IAP by default; the list can be narrowed to groups or
  individuals (`list_app_access`, `grant_app_access`, `revoke_app_access`), and
  the owner must always stay on it. Who may *use* the app is still decided by
  the app itself (§7).
- Cloud Run: 1 vCPU, 2 GB, min 0 / max 2 instances, concurrency 80, request
  timeout 300 s, ingress = internal + load balancer. Managed by the platform.
- Pre-provisioned, optional: **Firestore** (the data store), **Cloud Storage**,
  **Vertex AI / Gemini**. **Secret Manager** for third-party credentials via
  `create_secret`. The platform's PostgreSQL is *not* enabled for this project;
  if the user genuinely needs relational data, a platform admin enables it in
  the vibe platform portal and this file switches to the PostgreSQL variant on
  the next `get_vibe_context`.

## 3. Hard rules (the app will not deploy or will not be accepted otherwise)

1. **All app code in `/container`**: Dockerfile, `config.env`, backend,
   frontend. CI builds `container/Dockerfile` with `container/` as context.
2. **One container image serves everything** — API and static frontend.
3. **Non-root user** in the runtime image.
4. **Port 8080** (`ASPNETCORE_URLS=http://0.0.0.0:8080`, or `--port 8080`).
5. **`GET /healthz` → 200 `{"status":"ok"}` without authentication.**
6. **Configuration via `container/config.env`** (`GCP_PROJECT_ID`,
   `GCS_BUCKET`, other non-secret settings). Committed, copied into the image,
   loaded at startup, validated eagerly.
7. **No secrets in the repo, the image, `config.env` or the data store.** GCP
   access (and the database) uses Application Default Credentials / IAM.
   Third-party secrets: Secret Manager via `create_secret`, mounted as env
   vars (§14).
8. **Identity from the IAP header only.** No login pages, OAuth flows, JWT
   checks, sessions. Compare emails case-insensitively.
9. **Every app has access control** (§7): an admin panel, an `admins` list,
   and — unless the user explicitly wants the whole company in — an
   `allowed_users` list. First visitor becomes admin. **It ships in the first
   deploy**: IAP lets the whole `alza.cz` domain reach the URL, so without the
   allow-list every employee can use the app and read its data.
10. **Base images through the Alza registry proxy**:
    `registry.alza.cz/mcr.microsoft.com/dotnet/sdk:10.0`,
    `registry.alza.cz/mcr.microsoft.com/dotnet/aspnet:10.0`,
    `registry.alza.cz/hub.docker.com/library/node:22-alpine`,
    `registry.alza.cz/hub.docker.com/library/python:3.12-slim`. Direct pulls
    from Docker Hub or MCR fail in CI.
11. **Never edit `.azuredevops/`.**
12. **Branches**: work on `feature/<NNN>-<short-name>`. CI/CD triggers on
    `feature/*`, `develop`, `hotfix/*`, `release/*`. **There is ONE Cloud Run
    service — whichever branch was pushed last is what users see.**
13. **No Docker on developer machines.** Verify with the language's own
    tools; CI builds the container.

## 4. Tech stack

| Layer | Default (new apps) |
|---|---|
| Language / runtime | C# on .NET 10 (LTS) |
| Web framework | ASP.NET Core Minimal APIs |
| Config | `IOptions<T>` + DataAnnotations, `ValidateOnStart()` |
| Frontend | React 19, TypeScript, Vite (current `create-vite`), Tailwind CSS 3, React Router 7 when needed |
| Tests | xUnit + `Microsoft.AspNetCore.Mvc.Testing`; Vitest for frontend logic |
| GCP SDKs (only what you need) | `Google.Cloud.Firestore`, `Google.Cloud.Storage.V1`, `Google.Cloud.AIPlatform.V1` |

**Existing apps keep their language.** Python (FastAPI + uvicorn) is the other
common stack on the platform; its equivalents are in Appendix A. Never
re-platform an existing app.

## 5. Repository layout

```
<name>App/
├── AGENTS.md  CLAUDE.md  tech.md  principles.md     # delivered by vibe-git-mcp (see .vibe/context.lock)
├── README.md  .gitignore
├── .azuredevops/           # platform-owned pipelines — do not touch
├── .vibe/                  # context.lock, last-redeploy marker
├── docs/                   # long documents the agent does not need every day
└── container/
    ├── Dockerfile  .dockerignore  config.env
    ├── backend/            # Backend.csproj, Program.cs, AppSettings.cs, ConfigEnvLoader.cs, Auth/, Access/, Endpoints/, Services/, Models/
    ├── backend.tests/
    └── frontend/           # Vite + React + TS + Tailwind; vite.config.ts proxies /api in dev
```

## 6. Backend patterns (C#)

### 6.1 `config.env` and settings

```csharp
// ConfigEnvLoader.cs — KEY=VALUE lines; real env vars win over the file
public static class ConfigEnvLoader
{
    public static void Load(ConfigurationManager configuration, string path = "config.env")
    {
        if (!File.Exists(path)) return;
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;
            var key = parts[0].Trim();
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                configuration[key] = parts[1].Trim();
        }
    }
}

public sealed class AppSettings
{
    [Required(AllowEmptyStrings = false)] public string GcpProjectId { get; set; } = "";
    public string GcsBucket { get; set; } = "";
    // Secrets mounted by set_runtime_config / create_secret arrive as env vars:
    // [Required] public string OpenAiApiKey { get; set; } = "";   // env OPENAI_API_KEY
}

// Program.cs (excerpt)
ConfigEnvLoader.Load(builder.Configuration);
builder.Services.AddOptions<AppSettings>()
    .Configure(s =>
    {
        s.GcpProjectId = builder.Configuration["GCP_PROJECT_ID"] ?? "";
        s.GcsBucket    = builder.Configuration["GCS_BUCKET"] ?? "";
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 6.2 Program.cs ordering (critical)

```csharp
var app = builder.Build();
app.MapGet("/healthz", () => Results.Json(new { status = "ok" }));   // 1. health, no auth
app.MapGroup("/api").RequireAppAccess().MapYourEndpoints();          // 2. API (access filter, §7)
app.UseDefaultFiles(); app.UseStaticFiles();                          // 3. static frontend
app.MapFallbackToFile("index.html");                                  // 4. SPA fallback — LAST
app.Run();
public partial class Program { }   // lets tests use WebApplicationFactory<Program>
```

### 6.3 Identity from IAP

```csharp
public sealed record IapUser(string Email, string DisplayName);

public static class IapUserReader
{
    public const string HeaderName = "X-Goog-Authenticated-User-Email";
    public static IapUser? TryRead(HttpContext context)
    {
        var raw = context.Request.Headers[HeaderName].FirstOrDefault() ?? "";
        var email = (raw.Contains(':') ? raw.Split(':', 2)[1] : raw).Trim().ToLowerInvariant(); // "accounts.google.com:user@alza.cz"
        return email.Length == 0 ? null : new IapUser(email, email.Split('@')[0]);
    }
}
```

Return `401` from `/api/*` when the header is missing. Do not verify the IAP
JWT; Cloud Run ingress only accepts traffic from the load balancer.

### 6.4 GCP clients

```csharp
builder.Services.AddSingleton(sp => FirestoreDb.Create(sp.GetRequiredService<IOptions<AppSettings>>().Value.GcpProjectId));
builder.Services.AddSingleton(_ => StorageClient.Create());
```

ADC everywhere: on Cloud Run the attached service account, locally the
developer's own account (`gcloud auth login --update-adc`). No conditional
code, no key files.

- **Firestore**: top-level collections; auto ids for user content, meaningful
  ids (lowercase email) for lookups. Backups are configured by the platform.
- **Cloud Storage**: bucket from `config.env`; object names like
  `uploads/{user}/{timestamp}-{filename}`; private bucket, serve via the API
  or signed URLs.
- **Vertex AI / Gemini**: region `europe-west4`, ADC, no API keys.

## 7. Access control — mandatory in every app

Two gates. **IAP** (platform side, §7.1) decides who can reach the URL at all;
**the app** (§7.2) decides who may use it and who is admin. Since IAP admits
the whole `alza.cz` domain by default, §7.2 is the only thing standing between
the app's data and every employee: ship it in the first deploy of every new
app, add it to every existing app that lacks it before any other work, and
keep the allow-list as the default mode ("everyone" is the user's explicit
choice, made in the admin panel).

### 7.1 IAP — who reaches the app

- Default for new projects: the whole organisation (plus the project's
  `GRP-GCP-L-PLAT-GENCODE-<NAME>-{ADM,DEV,READ}` groups). The platform may
  narrow this later, so treat the IAP list as something you can read and
  change: `list_app_access`, `grant_app_access(members=[...])`,
  `revoke_app_access(members=[...])`. Changes are live within a minute.
- **The project owner (the signed-in user) must always have IAP access** —
  `grant_app_access` adds them automatically and `revoke_app_access` refuses
  to remove them.
- Symptom that IAP is the blocker: the user sees Google's "You don't have
  access" page instead of the app. Symptom that the app is the blocker: the
  app's own "no access — ask an admin" page (§7.2).
- If the platform service account lacks rights in the project, the tools say
  so and give the `gcloud beta iap web add-iam-policy-binding` fallback the
  signed-in user can run.

### 7.2 In the app — who may use it

- Firestore collection **`admins`**: document id = lowercase email. If the
  collection is empty, the **first visitor is added automatically** (so the
  app is usable right after deploy) and told they are the admin.
- Firestore collection **`allowed_users`**: document id = lowercase email. A
  request from a user who is neither admin nor allowed gets `403` from `/api/*`
  and the frontend shows a "no access — ask an admin" page. The app may
  instead be *open to the whole organisation* (`settings/access.mode =
  "everyone"`), but only if the user explicitly asks for that; the default is
  the allow-list.
- **Admin panel** (`/admin`, admins only): list, add and remove allowed users
  and admins; toggle the access mode. This is how "add user X" is normally
  done — by the user in the UI, or by the agent through the same API. Add the
  IAP grant (§7.1) only when X cannot reach the URL at all.
- Implement as an endpoint filter (`RequireAppAccess()` above) so every
  `/api/*` route is covered by default; `/healthz` stays public.
- Emails are compared case-insensitively; store them lowercase.

## 8. Frontend patterns

- `vite.config.ts` proxies `/api` to `http://127.0.0.1:8000` in dev and injects
  `X-Goog-Authenticated-User-Email: accounts.google.com:dev@alza.cz`.
- Always call the API with relative URLs (`fetch('/api/items')`).
- `npm run build` outputs `dist/`, which the Dockerfile copies to `wwwroot/`.

## 9. Dockerfile (built by CI only)

```dockerfile
FROM registry.alza.cz/hub.docker.com/library/node:22-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM registry.alza.cz/mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY backend/Backend.csproj ./backend/
RUN dotnet restore backend/Backend.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/Backend.csproj -c Release -o /app/publish

FROM registry.alza.cz/mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /app/frontend/dist ./wwwroot/
COPY config.env ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Backend.dll"]
```

CI also runs Hadolint, Trivy and Sonar. Keep the Dockerfile simple and pinned.

## 10. Local development (no Docker)

```bash
gcloud auth login --update-adc                      # once: user login + ADC in one step
gcloud config set project <gcp project id>
gcloud auth application-default set-quota-project <gcp project id>

cd container/backend  && dotnet watch run --urls http://localhost:8000
cd container/frontend && npm run dev                # http://localhost:5173, proxies /api
cd container/backend.tests && dotnet test
```

The developer's own account has Firestore/Storage/Vertex permissions, so local
code talks to the real project resources. No impersonation. Secrets for local
runs: `export OPENAI_API_KEY=...` in the shell for the session, never in
`config.env`.

## 11. CI/CD

Pushing a deploying branch runs `<name>App-ci` (build image → shared Artifact
Registry) which triggers `<name>App-cd` (deploy to the one Cloud Run
service). ~5–10 minutes end to end. Nothing to configure.

---

# Operations — through the vibe-git-mcp tools

| Need | Tool |
|---|---|
| Project identifiers, app URL, config.env values, platform rules | `get_vibe_project_info` |
| Clone URL and git setup | `get_vibe_repo` |
| Is my push deployed? What is live right now? | `get_deploy_status(branch=...)` → `state`, `failed_steps`, `currently_deployed`, `app_url` |
| Why did it fail? | `get_deploy_log(run_id, step?)` |
| History of deploys | `list_deploys(branch?)` |
| Store an API key / password | `create_secret(name, value, env_var)` |
| Set / read env vars and secret mounts | `set_runtime_config`, `get_runtime_config` |
| Who can reach the app through IAP; admit / remove someone | `list_app_access`, `grant_app_access`, `revoke_app_access` |
| Apply config without a code change | `redeploy(branch)` |

## 12. After every push

1. `get_deploy_status(project, branch=<your branch>)` every 30–60 s until
   `state` is `succeeded` or `failed` (~5–10 min). The response also carries
   `currently_deployed` — the branch and commit the ONE Cloud Run service is
   serving right now — and `app_url`.
2. On `failed`: `get_deploy_log(run_id)` returns the failed steps' log tails.
   Fix, push, watch again.
3. Tell the user: "Deployed — open `https://<name>.vibe.alza.cz`." You cannot
   open it yourself (IAP sign-in).

Runtime diagnostics (the signed-in developer may run these):

```bash
gcloud run services logs read <service> --region europe-west4 --project <gcp project id> --limit 50
gcloud logging read 'resource.type="cloud_run_revision" AND resource.labels.service_name="<service>" AND severity>=ERROR' \
  --project <gcp project id> --freshness=2h --limit 50
```

## 13. Before every push — the one-service rule

`get_deploy_status` → `currently_deployed.branch`. If it is not the branch you
are about to push and it contains newer work, stop and ask the user: pushing
replaces what users see. On an existing project always work from the deployed
branch. Stale `feature/*` branches must never be pushed.

## 14. Secrets and runtime configuration

1. Ask the user to paste the value in chat (never into a file).
2. `create_secret(name="openai-api-key", value=<pasted>, env_var="OPENAI_API_KEY")`
   — stores it regionally in Secret Manager, grants the app's service account
   read access, and writes the mount into the runtime config.
3. `redeploy(branch=<deployed branch>)` — the variable exists on the next
   revision. Then poll `get_deploy_status`.
4. In code read it from configuration (`builder.Configuration["OPENAI_API_KEY"]`,
   `[Required]` in `AppSettings`). A mounted secret with no version fails the
   deploy — `create_secret` always adds one.
5. Rotation: `create_secret` again with the same name (new version, `:latest`
   picks it up on the next deploy).

Non-secret runtime overrides: `set_runtime_config(env_vars={...})`, or better
`container/config.env` in the repo. `get_runtime_config` shows both files;
secret values are never returned.

If `create_secret` reports the platform lacks permission in the project, do
what it says: the signed-in developer can create the secret with `gcloud` and
then `set_runtime_config(secrets={"ENV": "secret-name:latest"})` mounts it.

## 15. Deployment checklist

- [ ] Everything under `container/`; Dockerfile uses `registry.alza.cz/...` bases
- [ ] Tests green and frontend build green locally
- [ ] Port 8080, non-root, `/healthz` public
- [ ] `config.env` has the values from `get_vibe_project_info`; no secrets anywhere
- [ ] Identity from `X-Goog-Authenticated-User-Email`; access control (§7) in place
- [ ] API under `/api/*`, relative URLs in the frontend, SPA fallback last
- [ ] `currently_deployed` checked; branch is `feature/...`; deploy watched and reported

---

## Appendix A — Python (FastAPI) equivalents

Same contract, for existing Python apps.

```dockerfile
FROM registry.alza.cz/hub.docker.com/library/node:22-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM registry.alza.cz/hub.docker.com/library/python:3.12-slim
RUN groupadd --system appuser && useradd --system --gid appuser --home-dir /app --shell /usr/sbin/nologin appuser
WORKDIR /app
COPY backend/requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt
COPY backend/app ./app
COPY config.env ./config.env
COPY --from=frontend-build /app/frontend/dist ./static
RUN chown -R appuser:appuser /app
USER appuser
EXPOSE 8080
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8080"]
```

```python
# app/config.py — load config.env, fail fast
import os
from pathlib import Path
from pydantic import BaseModel, Field

def load_config_env(path: str = "config.env") -> None:
    p = Path(path)
    if not p.exists():
        return
    for raw in p.read_text().splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        os.environ.setdefault(key.strip(), value.strip())   # real env vars win

class Settings(BaseModel):
    gcp_project_id: str = Field(min_length=1)
    gcs_bucket: str = ""

def get_settings() -> Settings:
    load_config_env()
    return Settings(gcp_project_id=os.environ.get("GCP_PROJECT_ID", ""),
                    gcs_bucket=os.environ.get("GCS_BUCKET", ""))
```

```python
# app/auth.py — identity from IAP
from fastapi import Header, HTTPException

def current_user_email(x_goog_authenticated_user_email: str | None = Header(default=None)) -> str:
    raw = x_goog_authenticated_user_email or ""
    email = raw.split(":", 1)[1] if ":" in raw else raw
    email = email.strip().lower()
    if not email:
        raise HTTPException(status_code=401, detail="Not authenticated")
    return email
```

```python
# app/main.py — healthz, API (with access dependency), static + SPA fallback last
from fastapi import Depends, FastAPI
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles
from google.cloud import firestore
from .auth import current_user_email
from .config import get_settings

settings = get_settings()
app = FastAPI()
db = firestore.Client(project=settings.gcp_project_id)

@app.get("/healthz")
def healthz():
    return {"status": "ok"}

@app.get("/api/me")
def me(email: str = Depends(current_user_email)):
    return {"email": email, "displayName": email.split("@")[0]}

app.mount("/assets", StaticFiles(directory="static/assets"), name="assets")

@app.get("/{full_path:path}")
def spa(full_path: str):
    return FileResponse("static/index.html")
```

Local dev: `uv venv && uv pip install -r requirements.txt && uvicorn app.main:app --reload --port 8000`;
tests with `pytest` + `httpx`. The access model of §7 applies unchanged
(Firestore `admins`, `allowed_users`, admin panel, access dependency on every
`/api/*` route).
