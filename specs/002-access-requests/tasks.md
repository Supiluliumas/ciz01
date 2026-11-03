# Tasks: Approved corporate access

## Phase 1: Setup
- [x] T001 Write specification and design in specs/002-access-requests/spec.md and plan.md.

## Phase 2: Foundations
- [x] T002 Add failing identity/access/review security tests in container/backend.tests/AccessRequestTests.cs.
- [x] T003 Add corporate identity validation and DTO/store contracts in container/backend/Models/Contracts.cs and Access/IAppStore.cs; exact domains alza.cz/alza.sk, one normalized email identity.

## Phase 3: Request access (US1)
- [x] T004 [US1] Implement idempotent own requests/status in container/backend/Access/MemoryAppStore.cs and FirestoreAppStore.cs; pending/rejected duplicates unchanged, revoked approval may reapply.
- [x] T005 [US1] Add exact authenticated self-service routes in container/backend/Endpoints/Api.cs.
- [x] T006 [US1] Implement request/status/error/retry page in container/frontend/src/AccessRequest.tsx and main.tsx.

## Phase 4: Review requests (US2)
- [x] T007 [US2] Add atomic admin review/list in both container/backend/Access stores; require pending, recheck actor admin, all reads before writes, grant user only, list maximum 100.
- [x] T008 [US2] Add review endpoints and UI in container/backend/Endpoints/Api.cs and container/frontend/src/Admin.tsx.

## Phase 5: Restricted access (US3)
- [x] T009 [US3] Remove everyone grants/options and recheck admin lists in container/backend/Access stores, Endpoints/Api.cs and container/frontend/src/Admin.tsx.
- [x] T010 [US3] Test request/decision/revocation/browser workflow in container/backend.tests/AccessRequestTests.cs and container/frontend/e2e/app.spec.ts.

## Phase 6: Verification
- [x] T011 Run full container/verify.sh and record docs/verification.md.
- [x] T012 Document owner-only IAP bootstrap, corporate request gate and final-name handoff in docs/access.md and docs/ready-to-deploy.md.

## Dependencies and implementation
T001 -> T002/T003 -> request store/routes/UI -> review -> restricted access -> verification.
Execute sequentially; models before stores, stores before endpoints, backend before UI.
Independent tests are described in spec.md/quickstart.md. No conflicting file
work is delegated. UI-only refinements could run independently after APIs settle.
MVP is US1 with all feature routes remaining denied; full delivery includes all
three stories. Cloud acceptance is excluded until user supplies project name.

## Phase 7: Bootstrap restriction (owner decision 2026-11-03)

- [x] T013 Restrict first-admin bootstrap to BOOTSTRAP_ADMIN_EMAIL in container/backend/Access stores per FR-012; cover denial-until-owner-initializes in container/backend.tests/ApiTests.cs.
