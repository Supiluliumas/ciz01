# Vibe Coding Platform — Setup Guide (`install.md`)

> **You are an AI coding agent (Claude Code, Codex, Cursor, Windsurf, VS Code Copilot
> or similar).** The person who gave you this file is a business user, not a
> developer. They will give you exactly **one** piece of information: their
> **vibe project name** (for example `trdctrl`) — or they will tell you they do
> **not have a project yet**, which is a normal starting point (section 4.4).
> Everything else in this document is derived from that name or comes from the
> platform itself.
>
> Your job: read this whole file once, then set up their computer **end to end
> without asking further questions**, unless something is genuinely blocked (a
> browser sign-in only the human can complete, a permission you cannot obtain).
> When you must involve the human, tell them the exact one thing to do, wait,
> then continue.
>
> At the end the user's coding agent (you) is connected to the platform's
> **vibe-git-mcp** server, the project repository is on disk with the platform's
> coding context in it, and you know what kind of repository you are looking at.
> From then on the user just describes what the app should do.

---

## 0. Ground rules for this setup run

1. **Ask for the project name if it was not given.** Accept any casing;
   normalise to lowercase (`TrdCtrl` → `trdctrl`). It contains only `a-z0-9-`.
   Call it `<name>` below. If the user has **no project yet** (they say so, or
   they do not know what you mean), do not guess or derive one: run sections 2,
   3 and 4 without a project and continue with the **No project yet** track in
   section 4.4 — the app gets built locally now and connected later.
2. **Do not ask anything else.** Every decision below is already made. If a
   command fails, try the next documented alternative, then something sensible
   of your own, and only then involve the human.
3. **Work in the current folder.** If it is empty you will clone the repo into
   it. If it already is the cloned repo, use it as is. If it contains an
   unrelated application, see section 6 (MIGRATE).
4. **Nothing global except prerequisites and the MCP registration.** Install
   tools (git, gcloud, Python, .NET, Node) system-wide or per-user as needed
   and register the vibe-git-mcp server with the user's agent (that is the
   point of this run). Do **not** create global agent skills or global
   instruction files. Everything project-specific goes inside the repo.
5. **No Azure DevOps credentials on this machine.** The user has no Azure
   DevOps licence and needs none. Git and CI/CD go through the platform's MCP
   server and its `vibe-git` wrapper with the user's own Google identity. Never
   ask for a PAT, never install the Azure CLI, never open dev.azure.com.
6. **No Docker on the user's machine.** The CI pipeline builds the image.
7. **speckit is optional and off by default.** Do not install `.specify/` or
   run `speckit.*` commands unless the user explicitly asks for it later.
8. **Talk to the user in the language they write in**, in plain business terms.
   Files you write are in English.
9. **Keep going until the platform answers you.** The setup is done when
   `list_my_vibe_projects` returns `<name>`, the repo is cloned, the platform
   context files are committed, you have reported the repository situation
   (section 6) and — for a NEW project — a first build went live. On the **No
   project yet** track it is done when the agent is connected
   (`list_my_vibe_projects` answers, even with an empty list), the generic
   context is committed in a local repo and the hello-world app runs locally;
   section 4.4 says how to continue the moment the project arrives.
10. Print a short progress line at each section boundary ("Prerequisites OK —
    now signing in to Google Cloud…").
11. **A project exists only when `list_my_vibe_projects` lists it.** The name
    the user gives you, the folder name, the derived ids of section 1 — none of
    that proves anything. Never tell the user their project "exists", "is
    provisioned" or "should be there"; if the list does not contain the name,
    say plainly that the platform does not know it for them and act on that
    (section 4.3).

---

## 1. What the platform is — in one minute

- Every vibe project is one GCP project, one git repository, one Cloud Run
  service, one public URL `https://<name>.vibe.alza.cz` behind Google IAP.
  Every identifier is derived from `<name>`; you never ask the user for ids.
- **vibe-git-mcp** (`https://azdomcp.vibe.alza.cz`) is the platform's MCP
  server. Through it you get: the project's identifiers and rules
  (`get_vibe_project_info`), the coding context every repo carries
  (`get_vibe_context`), the clone URL (`get_vibe_repo`), CI/CD status and logs
  (`get_deploy_status`, `get_deploy_log`, `list_deploys`), secrets and runtime
  configuration (`create_secret`, `set_runtime_config`, `redeploy`) and IAP
  access (`list_app_access`, `grant_app_access`, `revoke_app_access`). Tools
  are defined on the server — new ones appear without reinstalling anything.
- **Git goes through the same server**: the clone URL is
  `https://azdomcp.vibe.alza.cz/git/<name>App` and every git command runs
  through the `vibe-git` wrapper, which mints short-lived tokens from the user's
  Google sign-in. Pushing a `feature/*` branch **is** deploying.
- **Data store**: some projects use Firestore, some have a dedicated PostgreSQL
  database on the platform's Cloud SQL instance. `get_vibe_project_info` tells
  you which (`data_store`) and `get_vibe_context` delivers the matching variant
  of the coding context. You do not choose; the platform did.
- **Who may use the app** is decided twice: IAP in front (the platform, managed
  with the access tools) and inside the app (an admin panel — every app has
  one). Never build a login page.

Derived names (for your own orientation; the MCP returns them all — and they
mean nothing until the project is listed by `list_my_vibe_projects`):

| Thing | Pattern |
|---|---|
| GCP project id | `l-plat-gencode-<name>` |
| Cloud Run service | `cr-gc1-l-plat-gencode-<name>-<name>app` |
| App service account | `sa-gcp-<name>@l-plat-gencode-<name>.iam.gserviceaccount.com` |
| Storage bucket | `bkt-gcp-l-plat-gencode-<name>-<name>` |
| Public URL | `https://<name>.vibe.alza.cz` |
| Git clone URL (through the MCP server) | `https://azdomcp.vibe.alza.cz/git/<name>App` |
| Platform PostgreSQL (only if enabled) | instance `l-plat-ai-vibelb:europe-west4:platform-db`, database `app_<name>` |

---

## 2. Prerequisites — check, then install what is missing

| Tool | Minimum | Why |
|---|---|---|
| **`gcloud` (Google Cloud CLI)** | current | the user's identity for everything: MCP transport, git, GCP resources — **mandatory** |
| **`git`** | 2.40+ | clone / commit / push (always through `vibe-git`) |
| **Python** | 3.11+ | runs the MCP shim and the `vibe-git` wrapper |
| `curl` | any | downloads the client bundle (PowerShell: `Invoke-WebRequest` works too) |
| `.NET SDK 8` + `Node 22 LTS` | NEW C# projects | build and test the default stack locally (install in section 6 once you know the track) |
| `cloud-sql-proxy` | only if the project uses PostgreSQL | local connection to the platform database (section 7) |

**Procedure**

1. Detect the OS and shell (Windows + PowerShell, macOS + zsh, Linux + bash).
2. Run the version commands (`gcloud --version`, `git --version`,
   `python3 --version` / `py -3 --version`). Missing or too old → install.
3. **Be creative about installation.** Users have different rights. Try in
   this order and move on when one works:
   - **Windows:** `winget install --id Google.CloudSDK`, `winget install --id Git.Git`,
     `winget install --id Python.Python.3.12`. If winget is blocked try `choco`
     or `scoop` if present. If nothing system-wide is allowed, use per-user
     installs: the gcloud zip installer into `%LOCALAPPDATA%\Google\Cloud SDK`,
     the Python "user install" from python.org (tick *Add to PATH*), portable
     Git. Add the folders to the *user* PATH and re-open the shell (or refresh
     `$env:PATH` for the current session).
   - **macOS:** Homebrew: `brew install git python@3.12` and
     `brew install --cask google-cloud-sdk`. Without Homebrew: the gcloud
     tarball into `~/google-cloud-sdk`, Python from python.org.
   - **Linux:** distro packages or the same per-user installs as macOS.
4. After installing, **re-run the version checks in a fresh shell**. On
   Windows the new PATH is often not visible in the current terminal.
5. Do not install Docker, the Azure CLI or speckit.

---

## 3. Sign in to Google Cloud (the only sign-in there is)

```bash
gcloud auth login --update-adc
```

- `--update-adc` also writes Application Default Credentials, so **do not** run
  `gcloud auth application-default login` separately. Everything — the MCP
  shim, `vibe-git`, the local app, the database — then runs as the user's own
  Alza account. **No service-account impersonation, no key files.**
- This opens a browser; only the human can complete it. Tell them: "A browser
  window opened — please sign in with your Alza Google account." If no browser
  can open (remote shell), add `--no-launch-browser` and relay the URL/code.
- Verify identity and tokens work:

```bash
gcloud auth list --filter=status:ACTIVE --format="value(account)"   # the user's @alza.cz address
gcloud auth print-identity-token >/dev/null && echo "identity token OK"
```

Set the project for convenience (harmless if the user lacks rights yet):

```bash
gcloud config set project "l-plat-gencode-<name>"
```

---

## 4. Install the vibe-git-mcp client and connect the agent

The client is a small Python bundle served by the platform. Downloading it
needs the user's Google sign-in (the server sits behind IAP), so do it after
section 3.

### 4.1 Download and unpack

macOS / Linux / Git Bash:

```bash
BASE=https://azdomcp.vibe.alza.cz
SA=vibe-git-gateway@l-plat-ai-azdomcp.iam.gserviceaccount.com
TOKEN=$(python3 - "$SA" "$BASE" <<'PY'
import json, shutil, subprocess, sys, time, urllib.request
sa, base = sys.argv[1], sys.argv[2].rstrip("/")
now = int(time.time())
payload = json.dumps({"iss": sa, "sub": sa, "aud": base + "/*", "iat": now - 60, "exp": now + 600})   # UTC; iat a minute back tolerates clock skew
gcloud = shutil.which("gcloud") or shutil.which("gcloud.cmd") or "gcloud"   # Windows has only gcloud.cmd
access = subprocess.run([gcloud, "auth", "print-access-token"], capture_output=True, text=True, check=True).stdout.strip()
req = urllib.request.Request(f"https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/{sa}:signJwt",
    data=json.dumps({"payload": payload}).encode(), headers={"Authorization": f"Bearer {access}", "Content-Type": "application/json"})
print(json.load(urllib.request.urlopen(req))["signedJwt"])
PY
)
mkdir -p "$HOME/vibe" && cd "$HOME/vibe"
curl -fsS -H "Authorization: Bearer $TOKEN" -H "X-On-Behalf-Of: $(gcloud auth print-identity-token)" \
     -o vibe-git-mcp.tar.gz "$BASE/client-bundle"
tar xzf vibe-git-mcp.tar.gz && cd vibe-git-mcp
```

Windows PowerShell (same thing):

```powershell
$BASE = "https://azdomcp.vibe.alza.cz"
$SA   = "vibe-git-gateway@l-plat-ai-azdomcp.iam.gserviceaccount.com"
$now  = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()    # NOT `Get-Date -UFormat %s`: Windows PowerShell 5.1 returns LOCAL time there and IAP rejects the token
$payload = @{ iss=$SA; sub=$SA; aud="$BASE/*"; iat=$now-60; exp=$now+600 } | ConvertTo-Json -Compress
$access  = (gcloud auth print-access-token).Trim()
$signed  = Invoke-RestMethod -Method Post -Uri "https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/$SA`:signJwt" `
  -Headers @{ Authorization = "Bearer $access" } -ContentType "application/json" -Body (@{ payload = $payload } | ConvertTo-Json -Compress)
New-Item -ItemType Directory -Force "$HOME\vibe" | Out-Null; Set-Location "$HOME\vibe"
Invoke-WebRequest -Uri "$BASE/client-bundle" -OutFile vibe-git-mcp.tar.gz `
  -Headers @{ Authorization = "Bearer $($signed.signedJwt)"; "X-On-Behalf-Of" = (gcloud auth print-identity-token).Trim() }
tar -xzf vibe-git-mcp.tar.gz; Set-Location vibe-git-mcp
```

Every `alza.cz` account may mint the gateway token and download the bundle —
**a project is not needed for this step.** If it fails anyway:

| Answer | Meaning / fix |
|---|---|
| `401 Invalid IAP credentials: Invalid Service Account JwT` | the token's `iat` lies in the future: the machine clock is off, or the epoch seconds were computed in *local* time (Windows PowerShell 5.1 `Get-Date -UFormat %s` does that — use the snippet above exactly, it uses UTC). Check the clock (`w32tm /resync` as admin on Windows, `sntp -sS time.google.com` on macOS) and retry. Nothing on the platform side is wrong. |
| `401 … Audience specified does not match requested endpoint` | `aud` must be `https://azdomcp.vibe.alza.cz/*` — with the `/*`. |
| `403` from the `signJwt` call | this Google account may not mint gateway tokens. All `alza.cz` accounts may; an `alza.sk` or other account is granted individually when it is registered as owner/admin of a project. Check `gcloud auth list` shows the intended account; otherwise ask the platform team (AI Enablement). |
| `403 Access denied. For user …` from IAP | IAP does not admit the gateway service account — platform side; tell AI Enablement. |

None of these means "the project does not exist": whether a project exists is
answered only by `list_my_vibe_projects` in section 4.3.

### 4.2 Register the MCP server with the user's agent

Pick the host the user actually uses. The scripts create a Python venv inside
the bundle folder, save the platform values to `~/.config/vibe-git-mcp/env`,
bind this working directory to project `<name>` (`VIBE_PROJECT`) and register
the server:

```bash
# macOS / Linux / Git Bash — hosts: claude | codex | cursor | windsurf | claude-desktop | vscode
VIBE_MCP_BASE_URL=https://azdomcp.vibe.alza.cz \
GATEWAY_SA_EMAIL=vibe-git-gateway@l-plat-ai-azdomcp.iam.gserviceaccount.com \
VIBE_PROJECT=<name> bash setup.sh claude
# no project yet (section 4.4): the same with an EMPTY binding — VIBE_PROJECT= bash setup.sh claude
```

```powershell
# Windows PowerShell (if scripts are blocked: powershell -ExecutionPolicy Bypass -File .\setup.ps1 ...)
.\setup.ps1 -Agent claude -Project <name> -BaseUrl https://azdomcp.vibe.alza.cz `
  -GatewaySa vibe-git-gateway@l-plat-ai-azdomcp.iam.gserviceaccount.com
# no project yet: replace `-Project <name>` with `-NoProject`
```

Re-running the setup in the same folder **replaces** the registration — that is
how the folder gets bound to the project later (section 4.4, step 6).

- `claude` and `codex` are registered automatically (`claude mcp add-json`,
  `codex mcp add`). For Cursor, Windsurf, Claude Desktop and VS Code the script
  prints the JSON snippet and the config file path — write it there yourself.
- **Claude Code scope:** `setup.sh` registers with `--scope local` (this
  directory). If the user works from several folders, re-run with
  `VIBE_MCP_SCOPE=user`.
- Then the agent must be **restarted** so it loads the new server. Tell the
  user: "Please restart <agent> (close and reopen it in this folder) and tell
  me when it is back." This is the one unavoidable pause in the setup.

### 4.3 Verify the connection

After the restart, call the MCP tool **`list_my_vibe_projects`**. Read the
answer literally — its `projects` list is the **only** truth about which
projects exist for this user, and its `note` tells you what to do:

| The list… | Meaning | What you do |
|---|---|---|
| contains `<name>` | the project exists and the user is its owner/admin | call **`get_vibe_project_info`** with `project=<name>` and keep the answer — it has every identifier, the `data_store` and the `config_env` block you will need. Continue with section 5. |
| is **empty** | the user has **no vibe project yet** | tell them so in one sentence and go to **section 4.4**. This is a normal state, not an error. |
| is not empty but lacks `<name>` | the project the user named **does not exist for them** — not registered, or they are not its owner/admin (the owner may have changed) | say exactly that; never say it exists. Ask whether one of the listed projects is the one they mean. If not: ask the platform team (AI Enablement) to check the registration of `<name>` and, meanwhile, work as in section 4.4. |

Troubleshooting:

| Symptom | Meaning / fix |
|---|---|
| the agent has no `vibe-git-mcp` tools | not restarted, or registered in another scope/folder — check `claude mcp list` / the host's MCP config |
| `this agent is not acting on behalf of a user…` | `gcloud auth login --update-adc` again, restart the agent |
| `you don't have access to vibe project '<name>'` | the project does not exist for this user (not registered, or they are not owner/admin) — treat as "not listed" above; never call it existing |
| `no vibe project specified` | the directory is not bound: re-run setup with `VIBE_PROJECT=<name>` (only when `<name>` is listed) or pass `project=` to every tool |
| `your IAP token expired` (git) | just re-run the `vibe-git` command; tokens are minted per call |

### 4.4 No project yet — build locally now, connect later

The user has no entry in `list_my_vibe_projects`. Registering a vibe project is
a **platform-admin action**: the AI Enablement team creates it in the vibe
platform portal with the user as owner, which also provisions the GCP project
and the git repository. That takes time — you do not wait for it.

1. **Tell the user**, in their language, in two sentences: the platform does
   not know a project for them yet; they should ask the AI Enablement team for
   a vibe project (they can propose the short name, e.g. `trdctrl`) with
   themselves as owner, and give you the name when they have it. Then say you
   are starting the app on their computer in the meantime.
2. **Get the generic context.** Call **`get_vibe_context`** without a project.
   You receive the Firestore variant of `AGENTS.md`, `CLAUDE.md`, `tech.md`,
   `principles.md`. In the current folder: `git init` (if it is not a repo),
   `git checkout -b feature/001-init`, write the files, write `version` to
   `.vibe/context.lock`, commit `chore: add vibe context <version>`. **No
   remote yet.** Read `AGENTS.md` and `tech.md` — they are authoritative from
   here on.
3. **Scaffold the hello world to the platform contract** from `tech.md` (C#
   default), exactly as for a new project: everything under `container/`, one
   Dockerfile, port 8080, `/healthz`, identity from the IAP header, **the
   admin panel with its allow-list in the first version**, no secrets anywhere.
   Two things are specific to this track:
   - `container/config.env` lists the expected keys (`GCP_PROJECT_ID`,
     `GCS_BUCKET`) with **empty values** under the comment
     `# filled in by the platform once the vibe project exists`. Never invent
     values.
   - **Every data access goes through one interface** with a Firestore
     implementation *and* an in-memory implementation selected only by the
     environment variable `VIBE_LOCAL=1` (`tech.md` §1). The project may turn
     out to use the platform's PostgreSQL instead of Firestore; then only the
     implementation changes.
   Run it locally with `VIBE_LOCAL=1` (`tech.md` §10), run the tests, commit.
4. **Build what the user wants**, still locally, in small commits on
   `feature/001-init`. Show progress by running the app at
   `http://localhost:8080`. Say every time that this runs on their computer
   only — nothing is deployed until the project exists.
5. **Never**: deploy anywhere else, add a login page, use a database the
   platform does not offer, put secrets in files, or push this repo to another
   remote.
6. **When the user comes back with the name** — the connect step:
   1. Call `list_my_vibe_projects`. If `<name>` is **not** listed, say so
      (rule 11) and stay on this track; do not continue.
   2. Re-run the client setup **bound to the project**:
      `VIBE_PROJECT=<name> bash ~/vibe/vibe-git-mcp/setup.sh claude`
      (PowerShell: `.\setup.ps1 -Agent claude -Project <name>`). Restart the
      agent.
   3. `get_vibe_project_info(project=<name>)` → write the `config_env` block
      into `container/config.env` (replacing the empty values). Note
      `data_store`.
   4. `get_vibe_context(project=<name>, known_version=<.vibe/context.lock>)`.
      If a new version comes back, write the files and the lock, commit. If
      `data_store` is `postgres`, follow section 7 and swap the data
      implementation to the platform PostgreSQL (migrations for
      `admins`/`allowed_users` included); run the tests again.
   5. `get_vibe_repo(project=<name>)` → connect and reconcile with the
      platform repo, which already contains `README.md`, `.gitignore` and
      `.azuredevops/` (the pipelines — they must survive):

      ```bash
      V=~/vibe/vibe-git-mcp/vibe-git          # Windows: & "$HOME\vibe\vibe-git-mcp\vibe-git.cmd"
      $V remote add origin https://azdomcp.vibe.alza.cz/git/<name>App
      $V fetch origin
      git rebase origin/main                   # replays your commits on the platform's initial commit
      # on a conflict: keep the platform's .azuredevops/ and .gitignore, keep your README if it says more
      ```
   6. Push and watch exactly as in section 8:
      `$V push -u origin feature/001-init`, then poll `get_deploy_status`.
   7. Finish with the section 6 report and section 9. From here on the project
      is an ordinary vibe project.

---

## 5. Get the repository and the platform context

1. Call **`get_vibe_repo`** (`project=<name>`). It returns the clone URL and the
   setup steps. Clone with the wrapper, never with plain `git`:

   ```bash
   ~/vibe/vibe-git-mcp/vibe-git clone https://azdomcp.vibe.alza.cz/git/<name>App .   # macOS / Linux / Git Bash
   ```
   ```powershell
   & "$HOME\vibe\vibe-git-mcp\vibe-git.cmd" clone https://azdomcp.vibe.alza.cz/git/<name>App .
   ```

   (Clone into the current folder if it is empty; otherwise into
   `./<name>App` and continue there. Plain `git clone/push/pull` against this
   remote fails — the tokens live for minutes and `vibe-git` mints them per
   command. Local commands like `git status`, `git add`, `git commit`,
   `git checkout` are plain git.)

   **Already cloned from the old platform** (the folder is the repo and
   `git remote -v` shows `dev.azure.com/alzasoft/platform-gencode/_git/<name>App`):
   keep the clone and its history, just move the remote to the platform proxy —
   nothing else about the repo changes:

   ```bash
   git remote set-url origin https://azdomcp.vibe.alza.cz/git/<name>App
   ~/vibe/vibe-git-mcp/vibe-git fetch --all --prune          # proves the new remote works
   ```

   The old Azure DevOps sign-in is no longer used; if git still offers it, remove
   the cached `dev.azure.com` credential (Windows Credential Manager / macOS
   Keychain) so it cannot interfere. The user can hand their Azure DevOps
   licence back.

2. Make `vibe-git` easy to reach: add `~/vibe/vibe-git-mcp` to the user PATH,
   or note the full path — you will type it a lot. `git config --global
   core.longpaths true` on Windows before installing frontend dependencies.

3. Call **`get_vibe_context`** with `project=<name>` (and `known_version` =
   the content of `.vibe/context.lock` if the repo has one). Write every
   returned file to its `path` in the repo root (overwriting), write `version`
   to `.vibe/context.lock`, and commit on a branch (section 6 decides which):
   `chore: update vibe context to <version>`. The files are `AGENTS.md`,
   `CLAUDE.md`, `tech.md`, `principles.md` — **read `AGENTS.md` and `tech.md`
   now**; from here on they are authoritative and this guide only tells you how
   to get started.

4. Write the `config_env` block from `get_vibe_project_info` into
   `container/config.env` (create the folder if the repo is empty; for an
   existing app merge — never delete keys the app already uses).

---

## 6. Decide what kind of repository this is — and what to do

Look at the repo (`git branch -r`, the tree of `main`/`develop` and of every
`feature/*` branch, `container/`, Dockerfiles, README) **and** call
**`get_deploy_status`** (`project=<name>`): `currently_deployed` tells you
which branch and commit the one Cloud Run service is serving right now.

| Situation | How you recognise it | What you do |
|---|---|---|
| **A — NEW / empty** | no `container/` on any branch; only `README.md`, `.gitignore`, `.azuredevops/`; `get_deploy_status` says no deploys or the placeholder | Scaffold the platform hello world from `tech.md` (C# default; **including** the access-control admin panel and — if `data_store` is `postgres` — the migrations that create `admins`/`allowed_users`), on `feature/001-init`. Deploy it (section 8). Then ask the user what the app should do. **Never deploy without the admin panel**: IAP admits the whole `alza.cz` domain, so an app without its allow-list is open to every employee. |
| **B — EXISTING vibe app** | `container/Dockerfile`, `/healthz`, identity from the IAP header, `config.env`, maybe an older `CLAUDE.md`/`tech.md` | **Never re-platform it.** Check out the `currently_deployed` branch, install *its* toolchain (Python, .NET, Node — read the Dockerfile), run its tests once, replace the guidance files with the fresh context (step 5.3) and commit. Prove CI/CD with a small harmless change (section 8) only if the user wants a deploy now. Stale `feature/*` branches are normal — never push them. |
| **C — UNGUARDED app** | there is an application, but it ignores the platform contract: own login page / passwords / sessions, secrets committed, no `/healthz`, Dockerfile not under `container/`, several containers, a second frontend deployment, data in files or SQLite, a database engine the platform does not offer | **Keep the language and the business logic; bring the plumbing to the contract**, one hard rule at a time (`tech.md` §3): one container in `container/`, port 8080, `/healthz`, identity from the IAP header (remove the login), secrets to `create_secret`, data to the project's data store (Firestore or the platform PostgreSQL — `data_store` decides), access control from `tech.md` §7. Tell the user plainly what was not compliant and that this is the first thing to fix before new features. Deploy when the app starts locally and passes its tests. |
| **D — MIGRATE a local app** | the current folder holds the user's own app (any structure, GitHub or no remote) and the platform repo is empty | Keep the language; rewrite the plumbing to the contract exactly as in C; point `origin` at the platform clone URL (`vibe-git remote set-url …`); keep the history; deploy. If the platform repo *also* has an app, stop and ask the one question: "Which one should be the app from now on?" |

Arriving here from section 4.4 (the app was built before the project
existed)? That is case **D** with the plumbing already right: the connect step
of 4.4 did the rebase and the push, so only the report remains.

Whatever the situation, the outcome of this section is a one-paragraph report
to the user in business language: what is in the repo, what is live at the URL,
which data store the project uses, and what you will do first.

---

## 7. The data store — check before you write any code

`get_vibe_project_info` → `data_store`:

- **`firestore`**: the app uses Firestore `(default)` in the project. Nothing
  to install. If the user clearly needs a relational database, tell them a
  platform admin can enable the platform's PostgreSQL for the project (vibe
  platform portal, "Database"); after that `get_vibe_context` delivers the
  PostgreSQL variant and `platform_database` fills in.
- **`postgres`**: the project owns database `app_<name>` on the platform's
  Cloud SQL instance. `platform_database` in the answer has everything: the
  instance connection name, the database, the IAM user the app connects as, the
  IAM user *the developer* connects as (their e-mail), how the app connects on
  Cloud Run (Auth Proxy in the container for .NET, connector library for
  Python/Node — `tech.md` §6.5) and how to connect locally. **There are no
  database passwords anywhere**; IAM is the authentication.
  First make sure the **Cloud SQL Admin API is enabled in the app's own
  project** — the proxy/connector calls it, both on Cloud Run and locally:

  ```bash
  gcloud services enable sqladmin.googleapis.com --project l-plat-gencode-<name>
  ```

  Then install the Cloud SQL Auth Proxy for local development and prove access:

  ```bash
  brew install cloud-sql-proxy          # macOS. Windows: download cloud-sql-proxy.x64.exe from
                                        # https://cloud.google.com/sql/docs/postgres/connect-auth-proxy (no winget package),
                                        # save it as cloud-sql-proxy.exe in a folder on the user PATH
  cloud-sql-proxy --auto-iam-authn --port 5432 l-plat-ai-vibelb:europe-west4:platform-db   # keep running
  # in another shell, user = the signed-in e-mail, no password:
  psql "host=127.0.0.1 port=5432 dbname=app_<name> user=<user e-mail> sslmode=disable" -c "select current_user"
  ```

  (no `psql`? any client works — DBeaver, or a two-line Python with `pg8000`.)
  Never pass `--private-ip`. If the connection is refused with a permission
  error the grant is not there yet (~3 minutes after the platform enabled the
  database or added the user as admin) — wait and retry, do not invent a
  password. The developer's own e-mail is the local `DB_IAM_USER` (export it in
  the shell; real environment variables win over `config.env`).

---

## 8. Smoke deploy — push, watch, confirm

Only on a `feature/*` branch, only after tests and the frontend build are green
locally, and only after checking `currently_deployed` (there is **one** Cloud
Run service — the branch pushed last is what users see):

```bash
~/vibe/vibe-git-mcp/vibe-git push -u origin feature/001-init
```

Then poll **`get_deploy_status`** (`project=<name>`, `branch=feature/001-init`)
every 30–60 s: CI (`<name>App-ci`) builds the image, CD (`<name>App-cd`)
deploys it; 5–10 minutes end to end. On `failed` read **`get_deploy_log`**
with the `run_id`, fix, push again. Never ask the user to check Azure DevOps or
whether it worked — you can see it. When `state` is `succeeded` and
`currently_deployed` shows your commit: "Deployed — open
https://<name>.vibe.alza.cz". The first visitor becomes the app's admin.

Runtime errors after a successful deploy: `gcloud run services logs read
cr-gc1-l-plat-gencode-<name>-<name>app --region europe-west4 --project
l-plat-gencode-<name> --limit 100`.

---

## 9. Finish

1. Move this file to `docs/install.md` in the repo (create `docs/` if
   needed) and commit — it is out of the way for day-to-day work but stays
   findable.
2. Summarise for the user, in their language: what is installed, that their
   agent is connected to the platform, what is in the repository, what is live,
   which data store the project uses, and one sentence on how to continue
   ("Tell me what the app should do; I build it, deploy it and give you the
   link.").
3. From now on **`AGENTS.md` and `tech.md` in the repo are the instructions**;
   this guide is not needed again on this machine. New tools appear on the MCP
   server without reinstalling; a new version of the context arrives through
   `get_vibe_context` at the start of a session.

---

## Appendix A — Coming from platform 1.0 (the "old platform")

Users who built an app before this MCP existed have a repo, a GCP project and a
URL that all stay exactly as they are. Only the way their machine talks to the
platform changes:

| Before (1.0) | Now (2.0) | What you do |
|---|---|---|
| Azure DevOps licence + AD sign-in for git | none — `vibe-git` through the MCP server with the Google sign-in | section 4; switch `origin` (section 5); the licence can be returned |
| `gcloud auth application-default login --impersonate-service-account=…` | `gcloud auth login --update-adc` as the user themselves | section 3; delete any `impersonate` hints from the repo's docs |
| `.md` files downloaded from the AI hub and copied by hand | `get_vibe_context` delivers and versions them (`.vibe/context.lock`) | section 5.3; the old copies are overwritten |
| `GCP_PROJECT_ID` / `GCS_BUCKET` / `SA_NAME` handed over by the platform team | `get_vibe_project_info` derives them | nothing to ask for; `SA_NAME` is not needed locally any more |
| watching pipelines on dev.azure.com | `get_deploy_status` / `get_deploy_log` | section 8 |
| IAP users via Service Desk tickets | `grant_app_access` / `revoke_app_access` + the app's admin panel | `AGENTS.md` |
| Firestore only | Firestore, or the platform PostgreSQL when enabled | section 7 |

Platform side: the project must be registered on the vibe platform with the
user as owner (that is what lets them mint tokens and gives them project
access). Existing gencode projects were registered in bulk; if
`list_my_vibe_projects` does not list `<name>`, the AI Enablement team adds the
user as owner/admin in the portal — it takes effect within about two minutes.
Their app is then the **EXISTING vibe app** case of section 6: nothing is
rewritten, work continues on the branch that is live.

## Appendix B — Windows / PowerShell notes

- Use PowerShell 7 if available (`pwsh`); Windows PowerShell 5.1 also works.
  Git Bash (installed with Git for Windows) runs every `bash` snippet in this
  guide unchanged, including `setup.sh` and `vibe-git`.
- Running `.ps1` files may be blocked by policy:
  `powershell -ExecutionPolicy Bypass -File .\setup.ps1 -Agent claude …` avoids
  changing the policy.
- `py -3` is the usual Python launcher; `python3`/`python` may point at the
  Microsoft Store stub, which is on PATH but only prints an install hint.
  `setup.sh` probes `python3`, `python` and `py -3` and uses the first real
  3.11+; if none works, install Python (`winget install --id Python.Python.3.12`)
  and re-run.
- Google Cloud CLI on Windows is `gcloud.cmd`; the bundle resolves it itself, but
  your own scripts must call it through the shell or `shutil.which("gcloud")`.
- Long paths: `git config --global core.longpaths true` before installing
  `node_modules`.
- If `gcloud` opens the wrong browser profile, use
  `gcloud auth login --update-adc --no-launch-browser` and relay the URL.
- Claude Code / Codex on Windows read the same JSON MCP config; `setup.ps1`
  registers with the venv's `Scripts\python.exe`.

## Appendix C — Facts worth knowing when something is odd

- The MCP server and git proxy live at `https://azdomcp.vibe.alza.cz`; the
  gateway service account the client impersonates is
  `vibe-git-gateway@l-plat-ai-azdomcp.iam.gserviceaccount.com`. Since
  2026-09-14 the whole `alza.cz` domain holds
  `roles/iam.serviceAccountTokenCreator` on it (so the bundle download and the
  project-less tools work before a project exists); owners/admins of registered
  projects are also granted individually, which covers `alza.sk` accounts. The
  gateway token alone gives no project access — the server authorizes the
  human behind the `X-On-Behalf-Of` header.
- Registering a project is a platform-admin action in the portal
  (`https://platform.vibe.alza.cz`): the AI Enablement team creates it with the
  user as owner; users cannot create projects themselves. A change of owner is
  visible to the MCP server within about two minutes.
- Project access, and which projects have a PostgreSQL database, come from the
  vibe platform (portal `https://platform.vibe.alza.cz`, platform admins) and
  reach the MCP server within about two minutes of a change.
- Base images in Dockerfiles must come through `registry.alza.cz/…` (Docker
  Hub, MCR, gcr.io and ghcr.io are proxied); direct pulls fail in CI.
- IAP admits the whole organisation to new projects by default; narrowing it is
  `grant_app_access` / `revoke_app_access`, and the project owner always keeps
  access. Who may *use* the app is the app's own admin panel.
- Secrets: `create_secret(name, value, env_var)` → `redeploy(branch)`; never in
  the repo, `config.env` or the data store.
