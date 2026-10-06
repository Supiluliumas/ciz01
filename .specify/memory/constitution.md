# AI Academy Mailer Constitution

Version: 1.0.0. Ratified and amended: 2026-10-06.

1. Follow the user assignment and the platform's AGENTS.md, tech.md, and
   principles.md. All seven academy requirements are in scope.
2. One Cloud Run container under container/, port 8080, public /healthz,
   frontend and API together. Base images use the company registry proxy.
3. IAP identity, default allow-list, admin panel, atomic first-admin bootstrap,
   and no removal of the final administrator.
4. Secrets in Secret Manager, mounted as environment variables. Never store
   credentials in source, frontend, data store, configuration files, or logs.
5. ADC and platform storage in production. Local memory and simulations require
   VIBE_LOCAL=1 and visible labeling; never set this in a production image.
6. No production business data. Read only the configured academy test table.
7. Human review before sending, no bulk delivery. Do not persist email bodies
   or prompts. The same send identifier never invokes n8n twice.
8. Write meaningful tests before implementing security and send contracts.
   Pass backend tests and frontend build before pushing.
9. Spec Kit specification, plan, and tasks precede implementation. Keep acceptance
   evidence accurate. Do not deploy into unrelated projects.
10. Apply all eleven principles from principles.md: simple reusable focused
    code, clear names, pinned dependencies, defensive boundaries, fail fast,
    tests, verified deployment, and access control.

Governance: check these gates during planning and verification. Scope changes
update the spec, plan, tasks, and evidence. Never hand-edit platform context.
