# Access request model

`access_requests/{normalized-email}`: email (exact corporate domain), status
(pending, approved, rejected), createdAt UTC timestamp, reviewedAt optional UTC
timestamp, reviewedBy optional normalized admin email. No free-text reason,
mail body, credentials or login session.

One document per identity. none -> pending -> approved/rejected. Pending/rejected
submission is idempotent. Once approval membership is removed, own status reports
revoked and a deliberate POST can replace it with a new pending request.
Effective status is approved whenever current admins/allowed_users grants access.

Approval grants only allowed_users, never admins. Reviewing admin must exist at
transaction time. A decision requires pending; competing resolved decisions
return conflict. Every Firestore read occurs before writes. Membership changes
and review decisions update settings/access sentinel to serialize concurrent
changes. Pending admin preview is bounded to 100; resolved requests not listed.
