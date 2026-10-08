# Canonical install compliance audit

Date: 2026-10-06, Europe/Prague. Branch: feature/001-init.
Scope: root install.md sections 0–9 and appendices, tech.md, principles.md,
repository configuration/code/docs, local prerequisites, Google identity,
platform connection/context, complete local build and tests, and Git history.

## Source precedence

The owner explicitly made root install.md canonical alongside tech.md and
principles.md. Apply explicit replacements of older setup rules in install.md;
otherwise retain the current technical contract and coding principles. User
instructions take precedence. Do not rewrite server-owned AGENTS.md, CLAUDE.md,
tech.md or principles.md; refresh them through get_vibe_context.

The two install.md copies are byte-identical to the provided source. Root
placement is retained by explicit owner instruction despite install section 9
normally moving the guide into docs. README and assignment.md record the rule.

## Setup and platform checks

| Reference | Finding and evidence | Status |
| --- | --- | --- |
| Section 0: scope and project truth | Dedicated name cizkovskyai approved; current platform list contains only shub, explicitly excluded by owner | Pre-project state |
| Section 0: local work | Work stays in current folder, feature/001-init, no remotes; prerequisites and MCP only installed outside repo | Pass |
| Section 0: Spec Kit off by default | Academy screenshot explicitly requires Spec Kit; toolkit/artifacts are project-local | Authorized exception |
| Sections 1 and 4.3: identifiers | No actual project/service/bucket IDs invented in runtime config; empty GCP_PROJECT_ID and GCS_BUCKET retained | Pass |
| Section 2: prerequisites | Git 2.55.0, Python 3.12.15, gcloud 587.0.0, .NET 10.0.401, Node 24.21.0; build and tests pass | Pass with Node version difference |
| Section 2: Node 22 LTS recommendation | Local machine has Node 24; CI Dockerfile uses prescribed proxy Node 22 image; local toolchain verified | Documented difference |
| Section 3: Google identity | martin.cizkovsky@alza.cz active; identity and ADC token requests succeeded with values discarded | Pass |
| Section 3 / appendix A: ADC | authorized_user ADC, no impersonation URL, no service-account key file in repo | Pass |
| tech section 10: quota project | ADC quota project absent; assign actual project after MCP confirms registration and IAM | Pending project |
| Section 4.1: client | Official bundle and Python 3.12 venv exist under ~/vibe/vibe-git-mcp, wrapper executable | Pass |
| Section 4.2: registration | Codex registration points to venv/module, correct base/gateway/OBO; VIBE_PROJECT empty | Correct pre-project binding |
| Section 4.2: native tools after restart | Native Vibe tools are absent from this session; registered client calls work through stdio subprocess | Native loading still requires Codex restart |
| Section 4.3: live platform answer | list_my_vibe_projects succeeded and returned only shub; cizkovskyai is not registered for this user or user lacks ownership/access | Pending platform team |
| Section 4.4: local-first | App running at loopback port 8088, public health succeeds, local simulation explicit; no cloud calls/emails during verification | Pass |
| Section 4.4: local port | Port 8080 occupied by existing SSH tunnel; no tunnel changed; VIBE_LOCAL_PORT overrides local port; cloud stays 8080 | Documented local difference |
| Sections 4.4 and 5: context | get_vibe_context known_version=4863456b58c9 returned up_to_date=true, generic Firestore variant | Pass |
| Sections 4.4 and 5: platform repo | No platform remote exists until project listed; reconcile initial repo and preserve pipelines and ignores | Pending project |
| Section 6: classification | Local app built before provisioning; future case D only if platform repo empty. Inspect branches and currently_deployed before reconciliation | Handoff corrected |
| Section 7: store selection | IAppStore isolates all app persistence, Firestore and memory implementations; memory selected only by shell VIBE_LOCAL=1 | Pass locally; actual variant pending |
| Section 8: deployment | No platform push, CI/CD or live URL; deployment tasks remain unchecked | Pending project and integration configuration |
| Section 9: truthful handoff | Local implementation and remaining external work recorded; no certification or live acceptance claimed | Pass |
| Appendices A–C | No old Azure PAT/CLI workflow, no developer impersonation, proxy images and Secret Manager runtime procedure retained | Pass in repo |

Only project-list results establish availability. The requested display name
prj-gcp-l-plat-gencode-cizkovskyai and Service Desk fields are request metadata,
not proof of provisioning. Group platform and project gencode come from the
owner's supplied platform-team correction, not fields documented in install.md.

## Application contract and code review

| Contract | Evidence |
| --- | --- |
| One container, code under container, port 8080, non-root | container/Dockerfile uses company registry proxy images, USER app, ASPNETCORE_URLS and EXPOSE 8080; API and built React files in same image |
| Public health | GET /healthz returns 200 with status ok without IAP identity |
| IAP identity only in cloud | API parses X-Goog-Authenticated-User-Email, normalizes email; no login, sessions, OAuth, JWT validation or own passwords |
| App access from first version | Default allow-list, atomic first-admin initialization, case-normalized member management and final-admin protection; memory tests pass |
| Every API requires access | Unknown API fallback now belongs to the protected API group; new anonymous/unlisted/owner tests fail before fix and pass after |
| Configuration fail-fast | config.env copied to image, real configuration overrides file; production checks required project/integration settings before serving; no silent demo fallback |
| Pre-project simulation | VIBE_LOCAL=1 only in shell/test configuration, visibly labeled; no GCP clients constructed in local branch |
| Data-store boundary | IAppStore wraps access state and send reservations; Firestore transactions and memory implementation; no persistent email body or prompt |
| Google integrations | ADC Google credential for Vertex and BigQuery, Firestore ADC; no API key or service-account key |
| Third-party secret | n8n custom Header Auth server-side, N8N_AUTH_TOKEN from runtime environment; readiness rejects secret in config.env; setup uses create_secret then deployment |
| Frontend contract | Relative /api requests, React static files served by backend, SPA fallback last, no separate frontend deployment |
| Principles | Scoped services, minimal pinned dependencies, boundary validation, redacted errors, meaningful tests, default access restriction; deployment remains required before claiming completion |
| Build context | Existing .dockerignore excludes caches, local build files, env files, tests and logs; added tools exclusion to avoid copying local verification utilities into build stages |
| Secrets review | Scanned 123 historical Git blobs for common credential/private-key patterns, no likely credentials found; this is a pattern scan, not proof against every possible secret format |

AppSettings implements eager validation directly rather than the illustrative
IOptions/DataAnnotations pattern from tech section 6.1; required settings fail
at startup and are covered by tests. No GCS client is added because this app
has no storage feature. GCS_BUCKET stays an empty platform-owned config key
until actual project metadata arrives; tech makes bucket validation optional.

## Verification performed

Complete `bash container/verify.sh` passed: locked dependency install, frontend
production build, locked backend restore, 41 backend tests, 9 workflow tests,
5 readiness tests, Release publish, public-health check and 12 browser/HTTP
tests against its isolated Kestrel server. Total 67 passing tests; own test
server shut down. No Docker, cloud resources or external email used.

Live Firestore transaction behavior/IAM, actual data-store variant, Vertex
model invocation, BigQuery table access, n8n import/execution, Secret Manager
mount, SMTP delivery, CI scans/build and Cloud Run ingress remain unverified.
They require the dedicated registered project and actual integration setup.

## Authorized exceptions and pending work

- Spec Kit is enabled because the supplied assignment explicitly requires it.
- Root install.md retained alongside unchanged docs copy by owner request.
- Private GitHub backup explicitly requested; this overrides install 4.4's
  other-remote ban. There is currently no remote and no uploaded repository;
  GitHub CLI sign-in remains pending. Platform deployment still uses vibe-git.
- Local simulation uses 8088 because 8080 is occupied; production remains 8080.
- Local Node 24 replaces the recommended Node 22 toolchain; CI uses Node 22.

Once ciz01 is listed: bind/restart Codex, fetch actual metadata/context,
set confirmed gcloud/ADC project, adapt store only if PostgreSQL is provisioned,
inspect deployed branch and platform repository, reconcile preserving pipelines,
configure non-secret integration settings and Secret Manager, run checks, push
only the intended branch through vibe-git, watch CI/CD to the exact commit,
and record real acceptance. Never deploy into shub.

## Subsequent naming correction

The owner finalized the project/assignment name as `ciz01` and reports the
request is in approval. This supersedes the earlier `cizkovskyai` proposal in
the historical audit above. Its full request display name is
`prj-gcp-l-plat-gencode-ciz01` (28 characters). The 30-character request-field
limit applies to that full display name, not only the derived GCP project ID.
Platform availability must still be established by list_my_vibe_projects.

## Subsequent GitHub review handoff — 2026-10-08

The owner explicitly requested a repository link for academy supervisors while
platform project access is blocked. GitHub authentication is now available as
Supiluliumas. Private repo: https://github.com/Supiluliumas/ciz01, remote github;
origin remains reserved for the platform repo. This supersedes the earlier
historical no-remote/pending-sign-in state. No cloud deployment is implied.
