# Access design decisions

Decision: IAP authenticates; individual application allow-list grants features.
Rationale: Corporate users must reach the request screen before approval.
Alternative: approved-only IAP gate requires a separate request channel.

Decision: Preserve first-admin bootstrap, restrict IAP to owner during initial
cloud setup and initialize before allowing colleagues to request.
Rationale: Platform contract mandates first visitor; owner-only bootstrap avoids
an unapproved person becoming administrator. Alternative own login is forbidden.

Decision: Trust authenticated exact alza.cz/alza.sk emails, never submitted email.
Rationale: Both corporate domains appear in platform setup. Recipients retain
existing general email validation. Subdomains/lookalikes are rejected.

Decision: One request document per email, atomic review/member grant, recheck
reviewer and read settings before writes. Lists recheck reviewer in the store.
Rationale: Prevent duplicate requests, approval races and stale admin privileges.

Decision: Rejected requests remain rejected on duplicate submission; revoked
approvals can be submitted again. Status uses current membership first.
Rationale: No automatic bypass of rejection, yet removed users may reapply.

Decision: Remove organisation-wide mode; old everyone state never grants access.
Rationale: User explicitly requires approval of concrete individual emails.
No new technology dependency or external notification is introduced.
