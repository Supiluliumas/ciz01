# Access API contract

All endpoints require normalized corporate identity from IAP. No own login.
GET/POST /api/access-request are the only membership exemptions, exact path and
method. Other /api routes retain allowed-user/admin checks and CSRF mutation checks.

- GET /api/access-request: own {email,status,allowed,isAdmin,localMode}; no other requests.
- POST /api/access-request: no identity input; records authenticated identity,
  returns same own status. Pending/rejected repeats are idempotent.
- GET /api/admin/requests: pending {email,status,createdAt,reviewedAt,reviewedBy}[]
  at most 100, admin store recheck.
- POST /api/admin/requests: {email,decision:"approve"|"reject"}; admin only,
  transaction recheck; pending required. Success 200; stale decision 409.
- PUT /api/admin/mode: compatibility accepts only allowlist; everyone returns 400.
- Admin membership mutations accept corporate emails only; outbound recipients
  remain general email addresses.

Missing/malformed/external identity: 401. Unapproved protected request: 403.
Non-admin review/list: 403. No external email notification is sent.
Revocation blocks subsequent authorization checks; it cannot undo previously
started or already delivered work.
