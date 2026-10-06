# Data Model

## Access

`admins/{lowercase-email}` and `allowed_users/{lowercase-email}` contain email.
`settings/access`: mode (`allowlist` or `everyone`), initialized, adminCount.
Every admin mutation reads/writes the same settings document in a transaction.
Default allowlist; atomic first visitor becomes admin; adminCount cannot reach 0.
Actor admin role is rechecked inside the mutation transaction.

## Send coordination

`send_attempts/{UUID}`: ownerEmail, fingerprint (SHA256 of recipient/subject/body),
status, createdAt. Status moves pending -> acknowledged / simulated / uncertain.
A repeated UUID returns its status; differing owner/fingerprint is rejected.
Reservation is committed before n8n; uncertain or pending records never resend.
No prompt, body, subject, or recipient is stored.

## Transient request models

Draft: recipient (single email, max 254), instructions (10–4000), tone enum
professional/friendly/concise, language enum cs/en.
Send: UUID requestId, recipient, subject (1–200, no newlines), body (1–10000).
Access mutation: normalized email, role admin/user, mode allowlist/everyone.
Preview: headings and at most 20 rows, nested values rendered as JSON text.
