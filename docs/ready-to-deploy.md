# Ready-to-deploy handoff

Assignment title: ciz01. Vibe project `ciz01` is registered to the owner.
GCP project `l-plat-gencode-ciz01` (europe-west4, Firestore), app URL
https://ciz01.vibe.alza.cz, Cloud Run `cr-gc1-l-plat-gencode-ciz01-ciz01app`,
service account `sa-gcp-ciz01@l-plat-gencode-ciz01.iam.gserviceaccount.com`.

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

## Access approval prepared

The app now accepts requests from authenticated corporate identities and grants
features only to individually approved members. No organisation-wide mode.
See [access.md](access.md) for tested scenarios and initial owner-only IAP
bootstrap before admitting other colleagues to the request page.

## Once the project is ready

Give the coding agent the short assigned Vibe project name. It will verify
that the platform lists it, fetch its actual identifiers and data-store variant,
reconcile with the platform repository, and preserve the company pipelines.
If it uses PostgreSQL instead of Firestore, the IAppStore implementation must
be adapted before deploying; this cannot be determined before registration.

Remaining inputs (the app deploys without them; the features stay "not
configured" until the whole group is present):

| Input | How it is used |
| --- | --- |
| Published workflow at https://n8n.dev.gcp.alza.cz/webhook/ciz01-mailer | N8N_WEBHOOK_URL (already set) |
| Header Auth credential named X-N8N-Token | Secret Manager mount N8N_AUTH_TOKEN; no Basic Auth; the platform team must create the secret and mount it — owner asked to request this from AI Enablement and add the version himself |
| Owner Outlook credential in n8n (from AI Enablement) | Fixed sender in the imported workflow |
| Supplied academy BigQuery table reference | BIGQUERY_PROJECT_ID / BIGQUERY_DATASET / BIGQUERY_TABLE; academy must grant the app service account read access |
| Vertex model in europe-west4 | VERTEX_MODEL=gemini-2.5-flash (dotted names allowed since 2026-11-03) |

The agent pushes the tested feature branch through vibe-git and watches CI/CD.
No app code change is needed to switch an integration on later: fill the config
and redeploy.

## Demonstration for academy review

1. IAP currently admits the whole organisation (the platform cannot narrow it
   yet), so `BOOTSTRAP_ADMIN_EMAIL=martin.cizkovsky@alza.cz` guarantees the
   first admin can only be the owner. Owner opens the deployed Vibe URL and
   verifies initial administrator status; colleagues then reach the request
   page and apply individually — the allow-list gates every feature anyway.
2. Show a colleague requesting access and an admin approving/rejecting requests.
   Verify a pending user cannot use features. Grant/revoke another administrator,
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

Google sign-in and ADC were renewed successfully on 2026-10-08 through
`gcloud auth login --update-adc`. Platform calls work again; generic context
4863456b58c9 was confirmed current. Project-specific context and connection
remain blocked by ciz01 not appearing in the user’s platform project list.
