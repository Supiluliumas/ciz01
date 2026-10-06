# Deploying the academy assignment

Application/assignment name: `cizkovsky_ai_academy_ukol`.
Owner-approved short platform name: `cizkovskyai` (availability and assignment pending).
Do not deploy into the unrelated `shub` project.

## Required external resources

AI Enablement registers a dedicated Vibe project with the user as owner. A
GCP project alone is insufficient. Use `list_my_vibe_projects` to verify it.
The long human-readable assignment name must not be used verbatim as the
short platform name: GCP IDs allow at most 30 characters and the platform's
`l-plat-gencode-` prefix consumes 15. Obtain actual identifiers from MCP.

The academy also supplies its n8n host and BigQuery test dataset/table. Configure
an owner SMTP mailbox in n8n. Choose a Vertex model actually available in the
assigned region. These dependencies are not present in this workspace.

## Connect the project

1. Verify `list_my_vibe_projects` lists the assigned short name.
2. Re-register the existing client for Codex with VIBE_PROJECT=<assigned name>.
   Restart Codex so its native MCP tools reload.
3. Fetch `get_vibe_project_info` and `get_vibe_context(project=...,known_version=...)`.
   Replace the platform context and lock only if refreshed. Write returned
   `config_env` keys to container/config.env, preserving app-specific keys.
4. If the actual store is PostgreSQL, implement IAppStore using the platform's
   IAM database variant before deployment. The current generic context is Firestore.
5. Obtain remote URL from `get_vibe_repo`. Use the existing `vibe-git` wrapper
   to add remote, fetch, and reconcile local feature/001-init with origin/main.
   Preserve `.azuredevops/` and the platform `.gitignore`. The owner explicitly
   authorized a private GitHub backup under remote `github`; keep the platform
   repository as `origin` and deploy through the platform pipeline.

## Integration configuration

Non-secret keys in `container/config.env` or `set_runtime_config`:

| Key | Value source |
| --- | --- |
| GCP_PROJECT_ID / GCS_BUCKET | Platform MCP config_env |
| VERTEX_LOCATION | europe-west4, per platform contract |
| VERTEX_MODEL | Approved available Gemini model |
| N8N_WEBHOOK_URL | Published production webhook URL |
| N8N_HEADER_NAME | X-N8N-Token, matching the n8n credential |
| BIGQUERY_PROJECT_ID | Academy-supplied test dataset project |
| BIGQUERY_DATASET / BIGQUERY_TABLE | Academy-supplied test table |

Use `create_secret(name="n8n-auth-token", value=<credential>,
env_var="N8N_AUTH_TOKEN")` to store and mount the header secret. Never write it
to config.env. Cloud startup fails when required configuration is absent;
it never falls back to demo mode. Do not set VIBE_LOCAL in runtime config.

Enable required APIs and grant the attached service account only appropriate
Firestore access, Vertex invocation, access to the specific secret, and
BigQuery read access to the supplied table (tables.get + tables.getData). This
implementation uses tabledata.list; no query job permission or write permission
is required. Use developer ADC locally and the attached service account in cloud.
No service-account keys or developer impersonation.

## Verify and deploy

Run `dotnet test container/backend.tests` and
`npm --prefix container/frontend run build`. CI builds container/Dockerfile
using company registry proxy images; no local Docker required. Cloud listens
on port 8080. The local demo defaults to 8088 because this machine's 8080 is
already occupied; override with VIBE_LOCAL_PORT if necessary.

Before push, inspect `get_deploy_status` and currently_deployed. Push only the
appropriate feature branch through vibe-git. Poll status and logs until the
specific commit is deployed. First authenticated visitor becomes administrator;
visit as the owner first. Default allow-list restricts further users.

Record real Vertex generation, Secret Manager mount, supplied test-table read,
email receipt, admin management, and deploy commit/URL in docs/verification.md.
An n8n 200 response alone does not prove inbox delivery. Local tests do not
prove live Firestore transactions or actual cloud IAM permissions.
