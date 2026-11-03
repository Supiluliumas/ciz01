# Feature Specification: AI Academy Mailer

**Feature Branch**: `feature/001-init`
**Created**: 2026-10-06
**Status**: Ready for implementation
**Input**: Build `ciz01`: an app drafting emails with AI
and sending reviewed emails through n8n, meeting the seven academy criteria.

## User Scenarios & Testing

### User Story 1 - Draft and send an email (Priority: P1)

An admitted colleague enters a recipient, an instruction, a tone, and a
language. An editable subject and plain-text body appear. The user reviews
the final recipient and content before explicitly confirming delivery.

**Why this priority**: This is the primary purpose of the application.
**Independent Test**: Draft, edit, confirm, and receive one test email. Local
simulation proves the UI flow only; real receipt is separate acceptance evidence.

**Acceptance Scenarios**:
1. **Given** valid instructions, **When** drafting, **Then** an editable subject
   and body appear and nothing is sent automatically.
2. **Given** a reviewed draft, **When** confirming send, **Then** the automation
   receives one request and the screen reports its acknowledged result.
3. **Given** a repeated send identifier, **When** retrying, **Then** the
   automation is not invoked again.
4. **Given** an unavailable service, **When** submitting, **Then** an error
   appears and no success is fabricated.

### User Story 2 - Manage access (Priority: P1)

The first authenticated visitor becomes administrator. Administrators can add
and remove administrators and allowed users. Access starts restricted to the
allow-list. Corporate colleagues may request access; administrators approve or
reject individual addresses. Organisation-wide access is disabled by the
2026-10-08 requirement in specs/002-access-requests/spec.md.

**Why this priority**: Access control is required from the first release.
**Independent Test**: Bootstrap one admin, deny a second user, admit that user,
and ensure ordinary users cannot edit access or remove the final admin.

**Acceptance Scenarios**:
1. **Given** an empty app, **When** two visitors arrive concurrently, **Then**
   exactly one becomes administrator.
2. **Given** an unlisted colleague, **When** using the app, **Then** access is
   denied from features and the colleague can submit/check their own request.
3. **Given** the final administrator, **When** removing them, **Then** the
   operation is rejected.

### User Story 3 - Demonstrate test data access (Priority: P2)

An admitted user explicitly loads up to 20 rows from the supplied academy test
table. Rows are not edited or automatically included in emails.

**Why this priority**: The academy requires a test-data read demonstration.
**Independent Test**: Load the configured table and see headings, rows, and an
empty state for an empty table.

**Acceptance Scenarios**:
1. **Given** a configured test table, **When** loading, **Then** its preview
   appears without writing data.
2. **Given** simulation, **When** loading, **Then** sample rows are identified
   as demonstration data.

### Edge Cases

- Invalid recipient, missing subject/body, or oversized inputs.
- Missing identity; local development identity must not appear in cloud mode.
- Blocked or malformed AI output; webhook errors and timeouts.
- Uncertain delivery is not automatically retried.
- Sending or test-data preview called while its integration is not configured
  reports "not configured" instead of fabricating a result.
- Reusing a send identifier with changed content is rejected.
- Unknown API paths return an API error, never frontend HTML.
- Local in-memory access settings reset on restart; disclose this.

## Requirements

### Functional Requirements

- **FR-001**: Individual plain-text emails only; no attachments, campaigns,
  inbox access, CRM, or production business data.
- **FR-002**: One valid recipient; instructions 10–4000 characters, subject
  1–200 characters, and body 1–10000 characters.
- **FR-003**: Czech and English; professional, friendly, or concise tones.
- **FR-004**: Require review and explicit confirmation before sending.
- **FR-005**: Report acknowledged, simulated, failed, or uncertain delivery;
  never equate automation acknowledgement with verified inbox receipt.
- **FR-006**: Enforce Story 2 access and administrator restrictions.
- **FR-007**: Do not persist email bodies or instructions in the app.
- **FR-008**: Persist only access settings and minimal send coordination
  (identifier, owner, fingerprint, timestamp, status), plus minimal access-request
  metadata (identity, status, creation/review timestamps and reviewer).
- **FR-009**: Read-only preview of at most 20 supplied test rows.
- **FR-010**: Responsive Czech screens with loading, empty, error,
  unauthorized, and confirmation states.
- **FR-011**: Clearly label simulations; local mode sends no real email.
- **FR-012**: Never expose automation authentication material to the browser.
- **FR-013**: n8n sending and the BigQuery preview are optional until their
  configuration group is complete: the app still starts, the affected screen
  and `/api/status` report the feature as not configured, sending is refused
  before any send coordination record is written, and core drafting and access
  control remain fully usable. Missing core configuration (project, model)
  still refuses startup; malformed supplied values likewise.

### Key Entities

- **User access**: normalized corporate email and role, individual allow-list only.
- **Access request**: corporate identity, status and creation/review metadata.
- **Draft**: recipient, instructions, tone, language, subject, body; transient.
- **Send attempt**: identifier, owner, content fingerprint, and result.
- **Test preview**: headings and up to 20 rows; transient, read-only.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Demonstrate draft, edit, review, and send; receive one email from
  the owner's address.
- **SC-002**: Repeating one send request five times creates one delivery action.
- **SC-003**: Unauthenticated, unlisted, and non-admin users are denied each
  protected action in automated acceptance tests.
- **SC-004**: One supplied test preview displays at most 20 rows.
- **SC-005**: Record evidence for all seven criteria; cloud criteria remain
  pending until demonstrated on the assigned project.

## Assumptions

- This is a new assignment unrelated to `shub`; a project must be assigned
  and registered with the platform owner.
- Czech and plain-text emails are reasonable defaults.
- Sender mailbox, webhook authentication, and supplied test table are external
  acceptance dependencies. Per the owner decision on 2026-11-03 the first cloud
  deploy proceeds without them: those features report "not configured" until
  the platform supplies the secret mount and the academy table.
- Reading academy test data is an exception to no business-data access.
- Simulation cannot establish certificate completion.
