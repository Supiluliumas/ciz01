# Feature Specification: Approved corporate access

**Feature Branch**: `feature/001-init` (pre-project local integration branch)
**Created**: 2026-10-08
**Status**: Ready for planning
**Input**: Only approved individual enterprise emails may use the app; a colleague first requests access. Project is approved externally; final assigned name will be supplied after local preparation/testing.

## User Scenarios & Testing

### User Story 1 - Request access (Priority: P1)

An authenticated colleague who is not approved sees their corporate identity,
can request access and can check the decision, without seeing email tools or data.

**Why this priority**: Corporate authentication alone must never grant app usage.
**Independent Test**: Unapproved colleague submits a request; protected features remain inaccessible.

**Acceptance Scenarios**:
1. Given an unapproved colleague, when opening the app, then only the access-request page is available.
2. Given no request, when requesting access twice, then one pending request exists for the authenticated identity.
3. Given a pending or rejected request, when trying email/data/admin actions, then access is denied.
4. Given a missing or non-corporate identity, when calling access-request endpoints, then no request is created.

### User Story 2 - Review requests (Priority: P1)

An administrator sees pending requests and approves or rejects specific addresses.

**Why this priority**: Access is an explicit individual decision.
**Independent Test**: Administrator approves one requester, rejects another; only the approved requester gains use.

**Acceptance Scenarios**:
1. Given a pending request, when approved, then exactly its identity gains ordinary-user access.
2. Given a rejected request, when checking status, then the colleague sees rejection and has no app access.
3. Given an ordinary user or unapproved colleague, when listing/reviewing requests, then access is denied.
4. Given two concurrent reviews, then one decision commits and the other sees a conflict.

### User Story 3 - Maintain restricted access (Priority: P1)

Administrators can revoke individual access; no setting opens the app to the whole company.

**Independent Test**: Remove a previously approved user and verify protected features immediately deny subsequent requests.

**Acceptance Scenarios**:
1. Given a removed user, then the next protected call is denied and a new access request can be submitted.
2. Given an old organisation-wide mode, then unlisted colleagues are still denied.
3. Given an attempted organisation-wide setting, then it is rejected.

### Edge Cases

Repeated submissions, forged submitted email, external/subdomain identities,
concurrent approval/rejection, revocation of reviewing admin, last-admin removal,
network failures, stale decision, already-authorized user, removed user reapplying,
first administrator initialization before requests are enabled for the company.

## Requirements

### Functional Requirements

- **FR-001**: Use existing corporate authentication; do not add passwords or a separate login system.
- **FR-002**: Only admins and approved exact corporate addresses may use email and data features.
- **FR-003**: Derive request identity from authenticated account; no editable applicant identity.
- **FR-004**: Allow applicants to submit and inspect only their own request; no request list or other personal data.
- **FR-005**: Administrators may list pending requests and approve/reject; ordinary users cannot review.
- **FR-006**: Duplicate pending/rejected submissions are idempotent; reapplication after revoked approval is permitted.
- **FR-007**: Approval and member grant occur together, rechecking administrator authorization at decision time.
- **FR-008**: Preserve last-admin protection; reject organisation-wide access, including legacy mode settings.
- **FR-009**: A pending/rejected request grants no email, AI, data or admin permission.
- **FR-010**: Show loading/error/retry states and explicit local-simulation status.
- **FR-011**: Test access boundaries, review transitions and browser flows before cloud connection.

### Key Entities

- Corporate identity: normalized enterprise email, supplied by authentication.
- Access request: email, pending/approved/rejected state, submission and review times, reviewing administrator.
- Approved member: existing individually granted user or administrator.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All tested unapproved paths to email, AI, data and administration are denied.
- **SC-002**: Repeated pending submissions create one request per identity.
- **SC-003**: Approval grants only the reviewed person access; rejection grants none.
- **SC-004**: Applicant can request and inspect their status without exposing other requests.
- **SC-005**: All existing local scenarios and new access-request tests pass.

## Assumptions

The supported enterprise identities are exact alza.cz and alza.sk domains,
consistent with platform setup. IAP may admit corporate users to reach the
request screen, while the app denies features until approved. Narrowing IAP to
approved users only would require an external access-request channel and is
not automatically done by the application. No notification emails are sent.
Admins may still explicitly add individual corporate members. Initial platform
IAP access must be limited to the owner until first-admin initialization; this
preserves the mandatory platform bootstrap without granting a stranger admin.
