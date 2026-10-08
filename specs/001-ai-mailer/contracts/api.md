# API Contract

All `/api/*` require IAP identity and app access; health is public. Production
never inserts identity. Local loopback requests may use a visibly labeled demo
identity when VIBE_LOCAL=1. JSON mutation requests require `X-App-Request: 1`
to prevent cross-origin form submission; no CORS enabled.

- GET /healthz: {status:ok}.
- GET /api/me: {email,isAdmin,localMode}.
- GET /api/status: non-secret integration modes/configuration availability.
- POST /api/drafts: {recipient,instructions,tone,language} -> {subject,body,simulated}.
- POST /api/send: {requestId,recipient,subject,body} -> {requestId,status,simulated}.
- GET /api/admin/access (admin): {admins,users,mode}.
- POST /api/admin/members (admin): {email,role}.
- DELETE /api/admin/members?email=...&role=... (admin).
- PUT /api/admin/mode (admin): {mode:"allowlist"}; organisation-wide mode rejected.
- GET /api/test-data: {columns,rows,simulated,source}.

Errors: JSON {message}, 400 validation, 401 identity, 403 access, 409 send-id
conflict/final admin removal/pending delivery, 502 external response error,
504 timeout/uncertain delivery. Unknown API routes are 404 JSON.

n8n POST: {requestId,to,subject,body,requestedBy}. Secret in configured header,
never Basic Auth. Required acknowledgement: {status:sent,requestId:<same UUID>}.
No retries or redirects. Failure after dispatch means uncertain; check workflow.

Authenticated self-service request and admin review endpoints are defined in
[002-access-requests/contracts/api.md](../../002-access-requests/contracts/api.md).
