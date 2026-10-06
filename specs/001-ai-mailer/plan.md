# Implementation Plan: AI Academy Mailer

Branch: `feature/001-init` | Date: 2026-10-06 | [Spec](spec.md)

## Summary

Single-container ASP.NET Core application with React screens for email creation,
access administration, and a BigQuery test preview. Production calls Vertex AI
and n8n; explicit local mode uses memory and clearly labeled simulations.

## Technical Context

- Language: C# / .NET 10; React 19, TypeScript, Vite, Tailwind CSS 3.
- Dependencies: Google.Cloud.Firestore (including Google.Apis.Auth), HttpClient;
  xUnit and Microsoft.AspNetCore.Mvc.Testing; Playwright for browser acceptance.
- Storage: Firestore behind IAppStore, memory only under VIBE_LOCAL=1.
- Target: one Linux Cloud Run container, 8080, 1 CPU / 2 GB.
- Performance: interactive forms; external requests bounded at 60 seconds;
  test preview capped at 20 rows. No bulk sending.
- Scope: three screens, individual email sending, access settings, no business data.

## Constitution Check

PASS before and after design: one container, approved stack, IAP, default
allow-list, atomic first admin and last-admin protection, mounted secret,
ADC, no persisted bodies, explicit simulation, tests before implementation.
Project identities and deployment are external pending dependencies, not invented.

## Project Structure

`container/backend/{Access,Services,Models,Endpoints}`; `container/backend.tests/`;
`container/frontend/src/`; `container/{Dockerfile,config.env,.dockerignore}`.
Specification and design in `specs/001-ai-mailer/`; operational guides in `docs/`.

## Implementation strategy

Access and validation first; then drafting/delivery contracts; then screens,
test preview, n8n workflow template, browser validation, and deployment guide.
Persist reservations before external delivery. Never send from a Firestore
transaction callback. Repeat identifiers do not resend; uncertain sends remain
uncertain and require manual checking of n8n before a new attempt.
