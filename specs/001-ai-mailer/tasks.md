# Tasks: AI Academy Mailer

## Phase 1: Setup

- [x] T001 Fetch platform context into AGENTS.md, tech.md, principles.md, .vibe/context.lock.
- [x] T002 Initialize Spec Kit and write specs/001-ai-mailer/spec.md and design artifacts.
- [ ] T003 Configure pinned dependencies, ignores, and container/config.env.

## Phase 2: Foundation and US2 access (P1)

- [ ] T004 [US2] Write failing access/validation tests in container/backend.tests/ApiTests.cs.
- [ ] T005 [US2] Implement IAP guard and validated models in container/backend/Models/Contracts.cs.
- [ ] T006 [US2] Implement transactional/memory access in container/backend/Access/IAppStore.cs and stores.
- [ ] T007 [US2] Implement admin APIs and UI in container/backend/Endpoints and container/frontend/src/Admin.tsx.

## Phase 3: US1 email (P1)

- [ ] T008 [US1] Write duplicate-send and external contract tests in container/backend.tests/DeliveryTests.cs.
- [ ] T009 [US1] Implement Vertex drafting and local simulation in container/backend/Services/Drafting.cs.
- [ ] T010 [US1] Implement Header Auth and reservation-based delivery in container/backend/Services/Delivery.cs.
- [ ] T011 [US1] Implement composer, review modal, and error states in container/frontend/src/Composer.tsx.
- [ ] T012 [US1] Provide importable workflow and setup guide in docs/n8n-workflow.json and docs/n8n.md.

## Phase 4: US3 data (P2)

- [ ] T013 [US3] Implement bounded read-only preview in container/backend/Services/TestData.cs.
- [ ] T014 [US3] Implement sample data screen in container/frontend/src/TestData.tsx.

## Phase 5: Verification and handoff

- [ ] T015 Add proxy Dockerfile, local runner, and docs/deployment.md.
- [ ] T016 Pass backend tests, frontend build, and browser acceptance; record docs/verification.md.
- [ ] T017 Verify assigned Vibe project, reconcile repo, configure cloud services, and deploy.
- [ ] T018 Demonstrate real Vertex, Secret Manager, academy BigQuery read, and email receipt in docs/verification.md.

## Dependencies and execution

Setup -> access foundation -> admin/email/data -> local verification -> cloud.
US2 is foundational; US1 then US3 can be tested independently. Research may run
in parallel with documentation. Implementation proceeds sequentially in this
workspace. MVP is access control plus drafting/review/delivery.

T017/T018 are externally dependent and must remain unchecked until evidenced.
