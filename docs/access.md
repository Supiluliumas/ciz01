# Corporate authentication and approved access

IAP performs corporate sign-in before Cloud Run. The app reads the trusted
X-Goog-Authenticated-User-Email header and accepts exact alza.cz/alza.sk
addresses. It has no passwords, login page, OAuth flow, sessions or JWT checks.
Cloud Run must retain platform internal/load-balancer-only ingress.

Only individually listed admins and allowed_users can use the app. Legacy
settings/access mode everyone is ignored. The API rejects attempts to enable
organisation-wide use; the UI has no such option. Removing membership denies
subsequent authorized calls, but cannot cancel previously started or delivered
work. Outbound email recipients are not restricted to corporate addresses.

## Request and review

An IAP-authenticated colleague without membership sees only an access-request
page. GET/POST /api/access-request are the only exact path/method exceptions to
app membership checks; both still require corporate identity. Identity is
fixed from IAP, never an editable/request-body email. Applicants see only their
own status. No notification emails are sent.

Submitting creates one pending request keyed by normalized email. Pending and
rejected repeats are idempotent. Administrators see up to 100 pending requests,
approve/reject exact identities, and refresh to see more. Approval grants only
ordinary-user membership, atomically with the decision. Review/list store
operations recheck the reviewing administrator. Conflicting decisions return
409 and require refreshing. Access removed after an earlier approval reports
revoked; the user may explicitly request again. Rejected users must contact a
supervisor/admin for reconsideration; admins can still explicitly add a member.

Request metadata is persisted in access_requests (identity, state, creation/
review timestamps and reviewer). No email body, prompt or credential is stored.
Firestore is the prepared store; actual project data_store must be checked,
and replaced behind IAppStore if platform PostgreSQL is provisioned instead.

## Initial deployment and IAP gates

The platform mandates first-authenticated-visitor administrator bootstrap.
Do not first expose an uninitialized app to the whole organisation.

1. User supplies assigned short project name. Verify platform registration,
   metadata/context, actual store and currently_deployed before connecting.
2. Read list_app_access. Before first deployment, grant/retain the owner and
   restrict the IAP policy to owner-only while the app initializes, removing
   broad organisation/group admission as needed. Platform tools preserve owner.
   If the platform cannot manage IAP (at the 2026-11-03 first deploy its
   service account lacked the project role to do so), set
   `BOOTSTRAP_ADMIN_EMAIL` to the owner instead: a stranger who opens the app
   first is denied, cannot bootstrap, and nothing initializes until the owner
   arrives. The allow-list then gates every feature regardless of IAP breadth.
3. Push only reconciled/tested branch through vibe-git and watch CI/CD to the
   exact deployed commit. Owner opens the Vibe URL and verifies admin status.
4. Only then admit intended corporate users/groups through IAP so they can
   reach the request page. The app still grants functional access individually.
5. Verify with separate real accounts: submit while denied, approve/reject,
   inspect own status, use features only when approved, revoke and verify denial.

Restricting IAP itself to already-approved individuals would block new
colleagues before they reach this app, so self-service requests would need a
separate external channel. This implementation supports requests inside the
app by separating IAP authentication from individual app authorization.
The app never calls IAM policy-changing tools on behalf of a request and
administrator approval never automatically changes the project's IAM roles.

## Local evidence and cloud limits

Full verify.sh passes 54 backend, 17 browser/HTTP, 9 workflow and 5 readiness
tests (85 total). Includes spoofed applicant identity, exact corporate domains,
unapproved feature denial, duplicate requests, non-admin review denial,
approval/rejection, concurrent decisions, revoked administrator store rechecks,
revocation/reapplication, CSRF checks, and stale-list/error behavior.
Memory implementation is exercised; Firestore implementation compiles and its
transaction ordering was reviewed, but actual Firestore IAM/transactions and
real IAP policies still require cloud acceptance. Local demo identities are
injected only in explicit loopback VIBE_LOCAL mode, never as cloud sign-in.
