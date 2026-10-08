# Implementation Plan: Approved corporate access

**Branch**: feature/001-init | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

Keep IAP as authentication, exact app membership as authorization. Add only
GET/POST /api/access-request as authenticated self-service exceptions to the
membership filter. Preserve /api/me contract. Add own-status request page and
admin review queue; force individual allow-list even if legacy mode is everyone.

## Technical Context

C# .NET 10 Minimal APIs, existing Firestore SDK, React 19/TypeScript/Tailwind,
xUnit and Playwright. No new dependencies. Firestore through IAppStore with
matching local memory implementation. One existing Cloud Run container.
Existing email/data flows remain gated. Pending admin list capped at 100 entries.
Actual project metadata and Firestore IAM are pending final name/connection;
these are external acceptance, not unresolved local design choices.

## Constitution Check

Before and after design: pass. Canonical install.md/tech/principles retained,
no login/OAuth/JWT/password addition, default individual allow-list, mandatory
first-admin bootstrap, last-admin protection, one container, ADC, no secrets,
no production business data. Only request metadata is persisted. Security tests
precede implementation. No platform deployment or external messages in local work.

## Phase 0: Research

Read-only design reviewer confirmed authentication/authorization distinction,
initial owner-only IAP bootstrap, exact path+method exemptions, corporate domain
validation independent of recipient validation, transaction reads before writes,
admin rechecks and membership-based status after revocation. See research.md.

## Phase 1: Design

See data-model.md, contracts/api.md and quickstart.md. Review decisions and member
grant use a Firestore transaction reading settings, reviewing admin, request and
member before writes; settings sentinel serializes against concurrent member
revocation. Lists recheck administrator inside store read transaction. Request
creation reads membership and existing request; pending/rejected are idempotent,
revoked previous approval may become pending again.

## Project Structure

- container/backend/Models/Contracts.cs: corporate validation/request DTOs
- container/backend/Access/IAppStore.cs and both stores: request persistence
- container/backend/Endpoints/Api.cs: exact self-service exemption/admin endpoints
- container/frontend/src/AccessRequest.tsx: applicant page
- container/frontend/src/Admin.tsx: pending review controls
- container/backend.tests/AccessRequestTests.cs: boundary and lifecycle tests
- container/frontend/e2e/app.spec.ts: browser/request/admin integration scenarios
- docs/access.md: two gates, owner bootstrap and eventual cloud acceptance

## Implementation Strategy

Sequential security tests, backend, applicant/admin UI, full verification,
documentation and commit. User will supply final project name when prepared.
