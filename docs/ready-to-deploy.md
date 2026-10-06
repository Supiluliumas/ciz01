# Ready-to-deploy handoff

Assignment title: ciz01. Owner-approved short Vibe name:
ciz01. The owner reports the dedicated project request is in approval;
provisioning and platform visibility are pending.

## Work already prepared

Application, approved container layout, default allow-list/admin panel, reviewed
email flow, read-only test preview, Vertex and n8n clients, transactional storage,
Secret Manager environment binding, pinned dependencies, Spec Kit artifacts,
n8n import template, and local verification commands.

From the repository root, the complete repeatable offline check is:

```bash
bash container/verify.sh
```

It installs locked frontend dependencies, builds, runs backend/workflow/readiness
tests, publishes a production build, starts a dedicated local simulation on
127.0.0.1:8099, runs browser tests, then shuts down only its own server. It does
not use GCP or n8n and does not send email. If port 8099 is busy, choose
`VIBE_VERIFY_PORT=8100 bash container/verify.sh`. Screenshots go to docs/screenshots.

Configuration-only deployment checks:

```bash
python3.12 container/tools/readiness.py
python3.12 container/tools/readiness.py --strict --json
```

Strict mode exits 2 while fields are missing. Output contains key names and
presence, never values, tokens, or credentials. Readiness is not a live IAM,
mailbox, Vertex, or Firestore check. Never put a secret in config.env just to
make this check pass.

## Once the project is ready

Give the coding agent the short assigned Vibe project name. It will verify
that the platform lists it, fetch its actual identifiers and data-store variant,
reconcile with the platform repository, and preserve the company pipelines.
If it uses PostgreSQL instead of Firestore, the IAppStore implementation must
be adapted before deploying; this cannot be determined before registration.

Other inputs may be prepared while the project is being provisioned:

| Input | How it is used |
| --- | --- |
| Production n8n webhook URL | N8N_WEBHOOK_URL |
| Header Auth credential named X-N8N-Token | Secret Manager mount N8N_AUTH_TOKEN; no Basic Auth |
| Owner SMTP mailbox and credential in n8n | Fixed sender in the imported workflow |
| Supplied academy BigQuery table reference | Read-only project / dataset / table configuration |
| Approved available Vertex model in europe-west4 | VERTEX_MODEL; determined against project permissions |

The agent will configure the secret through platform tools, run configuration
checks, verify APIs and permissions, push the tested feature branch through
vibe-git, and watch CI/CD. No app code change should be needed for integration
identifiers; the actual store variant and provider contracts still need checking.

## Demonstration for academy review

1. Owner opens the deployed Vibe URL and becomes initial administrator.
2. Show administration: admit a colleague, grant/revoke another administrator,
   and demonstrate that the final administrator cannot be removed.
3. Enter a recipient, Czech instructions, tone, and language. Generate a real
   Vertex draft; edit subject/body. Nothing has been sent yet.
4. Show the review dialog and explicitly confirm. Observe n8n execution and
   verify one actual email arrives from the owner's mailbox.
5. Show Secret Manager configuration/mount without displaying the secret value.
6. Load the supplied BigQuery test preview: at most 20 rows, no data writes.
7. Show the Spec Kit constitution, spec, plan, tasks, and passing tests.

Record deployment URL, commit, actual service checks, and receipt evidence in
verification.md. Do not claim delivery just because an HTTP request succeeded.

## Known limits until live acceptance

The local store resets at restart. Simulation is visible and never represents
live cloud evidence. A failed/timeout send identifier is not automatically
retried; check n8n before starting a new email. Cloud Run ingress must remain
restricted to the platform load balancer: trusted forwarded HTTPS scheme and
IAP identity rely on that platform contract.

Forwarded scheme handling follows [ASP.NET Core forwarded headers guidance](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/8/forwarded-headers-unknown-proxies?view=aspnetcore-10.0).
Only X-Forwarded-Proto is processed, one hop; forwarded host and identity are
not adopted. Local mode keeps the default known-proxy restrictions.
