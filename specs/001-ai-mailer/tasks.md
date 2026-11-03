# Tasks: AI Academy Mailer

## Phase 1: Setup

- [x] T001 Fetch platform context into AGENTS.md, tech.md, principles.md, .vibe/context.lock.
- [x] T002 Initialize Spec Kit and write specs/001-ai-mailer/spec.md and design artifacts.
- [x] T003 Configure pinned dependencies, ignores, and container/config.env.

## Phase 2: Foundation and US2 access (P1)

- [x] T004 [US2] Write failing access/validation tests in container/backend.tests/ApiTests.cs.
- [x] T005 [US2] Implement IAP guard and validated models in container/backend/Models/Contracts.cs.
- [x] T006 [US2] Implement transactional/memory access in container/backend/Access/IAppStore.cs and stores.
- [x] T007 [US2] Implement admin APIs and UI in container/backend/Endpoints and container/frontend/src/Admin.tsx.

## Phase 3: US1 email (P1)

- [x] T008 [US1] Write duplicate-send and external contract tests in container/backend.tests/DeliveryTests.cs.
- [x] T009 [US1] Implement Vertex drafting and local simulation in container/backend/Services/Drafting.cs.
- [x] T010 [US1] Implement Header Auth and reservation-based delivery in container/backend/Services/Delivery.cs.
- [x] T011 [US1] Implement composer, review modal, and error states in container/frontend/src/Composer.tsx.
- [x] T012 [US1] Provide importable workflow and setup guide in docs/n8n-workflow.json and docs/n8n.md.

## Phase 4: US3 data (P2)

- [x] T013 [US3] Implement bounded read-only preview in container/backend/Services/TestData.cs.
- [x] T014 [US3] Implement sample data screen in container/frontend/src/TestData.tsx.

## Phase 5: Verification and handoff

- [x] T015 Add proxy Dockerfile, local runner, and docs/deployment.md.
- [x] T016 Pass backend tests, frontend build, and browser acceptance; record docs/verification.md.
- [ ] T017 Verify assigned Vibe project, reconcile repo, configure cloud services, and deploy.
- [ ] T018 Demonstrate real Vertex, Secret Manager, academy BigQuery read, and email receipt in docs/verification.md.

## Dependencies and execution

Setup -> access foundation -> admin/email/data -> local verification -> cloud.
US2 is foundational; US1 then US3 can be tested independently. Research may run
in parallel with documentation. Implementation proceeds sequentially in this
workspace. MVP is access control plus drafting/review/delivery.

T017/T018 are externally dependent and must remain unchecked until evidenced.

## Phase 6: Convergence

- [x] T019 CRITICAL Handle trusted load-balancer HTTPS forwarding in container/backend/Program.cs and test request origin checks in container/backend.tests/RequestSecurityTests.cs per Constitution 2 and US1/AC2 (contradicts).
- [x] T020 Verify maximum input lengths and Unicode JSON bodies in container/backend.tests/ApiTests.cs and container/backend/Program.cs per FR-002 (partial).
- [x] T021 Exercise blocked, unavailable, empty, and uncertain states in container/frontend/e2e/app.spec.ts; add retry/uncertain-result guidance in container/frontend/src per FR-005 and FR-010 (partial).
- [x] T022 Add reproducible local verification and non-secret deployment readiness reporting in container/verify.sh and container/tools/readiness.py with docs/ready-to-deploy.md per plan: local verification and T017 (partial).

## Phase 7: Canonical install compliance

- [x] T023 Audit root install.md against setup, platform context, repository and application; record docs/install-audit.md, protect unknown API paths under the access filter, refine build-context exclusions, and pass full local verification.

T017/T018 remain pending: project registration, actual data-store selection,
platform reconciliation, CI/CD, secret mount and live integration acceptance.

## Phase 8: First-deploy hardening (owner decision 2026-11-03)

- [x] T024 Make n8n sending and the BigQuery preview optional until configured per FR-013: core settings required, integration groups all-or-nothing, AppSettings, DeliveryService and BigQueryTestData guards, truthful /api/status.
- [x] T025 Surface "not configured" on the affected screens in container/frontend/src (Composer, TestData) and cover with container/frontend/e2e/app.spec.ts.
- [x] T026 Split readiness into core keys vs integration groups in container/tools/readiness.py with container/tools/test_readiness.py coverage.
