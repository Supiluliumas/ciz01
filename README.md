# ciz01

AI Academy test application: AI email drafting, human review, and sending via
an n8n Header Auth webhook. Includes administrator access management and a
read-only preview of the academy's BigQuery test table.

## Repository for academy review

Private GitHub repository: https://github.com/Supiluliumas/ciz01

The application is implemented and locally verified. Review
[verification evidence](docs/verification.md), [access approval](docs/access.md),
[deployment handoff](docs/ready-to-deploy.md), and both Spec Kit feature folders.
The complete local suite has 88 passing tests. Cloud deployment is blocked by
platform registration/access: ciz01 is not yet visible for the owner's account.
Real Vertex, n8n delivery, Secret Manager and BigQuery acceptance remain pending.
Reviewers need permission to this private repository; publishing code here does
not deploy the application or change platform access.

## Canonical platform instructions

Use root [install.md](install.md) together with [tech.md](tech.md) and
[principles.md](principles.md), as explicitly directed by the owner. Explicit
setup replacements in install.md take precedence over older setup rules;
remaining technical and coding requirements continue to apply. Server-managed
context must only be refreshed through MCP. See [the compliance audit](docs/install-audit.md)
for verified evidence, authorized exceptions, and pending platform steps.

## Run locally

Requires .NET 10 and Node 22+. From repository root:

```bash
bash container/run-local.sh
```

Open http://localhost:8088. This is an explicit **local simulation**: no AI
request, no email sent, and no cloud data read. Access settings are in memory
and reset on restart. The first loopback visitor uses demo@alza.cz as a visibly
labeled development identity. Cloud mode never inserts a development identity.

```bash
dotnet test container/backend.tests
npm --prefix container/frontend run build
npm --prefix container/frontend run test:e2e
```

Browser tests expect the local app to be running on port 8088. Install its
Chromium dependency once with `npx --prefix container/frontend playwright install chromium`.

## What is implemented

- Czech responsive composer with recipient, instruction, tone, language,
  editable subject/body, explicit review dialog, and delivery acknowledgement.
- IAP identity and default allow-list, first-admin bootstrap, administrator and
  allowed-user management, access requests with admin approval/rejection, and
  last-admin protection. Only approved individual corporate emails may use features.
- Production Vertex REST integration using ADC and structured output.
- Server-side n8n Header Auth; Secret Manager-mounted N8N_AUTH_TOKEN.
- Firestore transactions for access and delivery coordination; no email-body
  persistence, no automatic retries, and duplicate-ID protection.
- BigQuery read-only preview capped at 20 rows; no business database access.
- Platform-compatible single-container Dockerfile, healthz, and configuration.
- Spec Kit workflows and specification/plan/tasks committed before app implementation.

## Corporate access requests

[IAP and access approval](docs/access.md): corporate users can reach the request
screen, but only individually approved accounts can use email/data features.
Admins review requests in Správa přístupu. First deployment must initially
restrict IAP to the owner until the administrator is initialized. No own login
page is added; the platform handles corporate sign-in.

## Pending external acceptance

Run the complete repeatable local verification with `bash container/verify.sh`.
It starts and shuts down its own isolated demo server; no existing app needs
to be running. The configuration-only readiness report is
`python3.12 container/tools/readiness.py --strict` (no values or secrets printed).
See the [ready-to-deploy handoff](docs/ready-to-deploy.md) for remaining inputs
and the academy demonstration sequence.

A dedicated Vibe project, project configuration, published n8n workflow and
owner mailbox, webhook secret mount, approved Vertex model, and the academy's
BigQuery test table are needed to demonstrate live requirements. No deployment
or certification completion is claimed by the local demo.

See [deployment](docs/deployment.md), [n8n setup](docs/n8n.md),
[assignment](docs/assignment.md), [specification](specs/001-ai-mailer/spec.md),
[implementation plan](specs/001-ai-mailer/plan.md), and
[verification evidence](docs/verification.md).

The owner authorized a private GitHub repository for this assignment. Use the
`github` remote for that backup; reserve `origin` for the dedicated Vibe repository
when provisioning finishes. Never connect this assignment to shub or another
unrelated project.
