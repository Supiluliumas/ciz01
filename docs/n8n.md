# n8n setup

The workflow runs on Alza's **DEV** n8n instance, `https://n8n.dev.gcp.alza.cz`
(the internal n8n guide: DEV is the working instance for everyone, really sends
email, and PRD is only for workflows that need internal production systems —
this one does not). Import `n8n-workflow.json`; it is an unconfigured template.
The agent has no connected n8n administration tool, so the owner performs the
steps in the n8n UI.

## Prerequisites (AI Enablement — Jakub Jakeš on Teams)

- n8n access: ask for a registration link. Sign-in is twofold: Google IAP with
  the corporate account, then the n8n account itself.
- **Microsoft Outlook credential** for the owner's mailbox. Microsoft
  credentials are created by the n8n admin, not by users. The e-mail is sent
  from the mailbox of this credential; the sender is never taken from input.

## Steps

1. Menu → **Import from File** → `docs/n8n-workflow.json`. Keep the name
   `ciz01 – odeslání e-mailu z aplikace přes Outlook`.
2. **Webhook z aplikace ciz01** — POST, path `ciz01-mailer`, Authentication
   **Header Auth**, respond via Respond to Webhook. Create a new *Header Auth*
   credential: Name `X-N8N-Token`, Value a strong random string (for example
   `openssl rand -hex 32`). Do not share the credential. The same value goes
   later to GCP Secret Manager as `N8N_AUTH_TOKEN` through the platform tools —
   never into this repository, chat, `config.env` or logs. Never Basic Auth.
3. **Odeslat e-mail (Outlook)** and **Upozornit vlastníka na chybu** — select
   the Outlook credential from step "Prerequisites". Never enable
   continue-on-error on the send node.
4. Workflow settings (gear icon) → **Error Workflow** → select this workflow
   itself, so **Při chybě workflow** notifies the owner. The internal guide
   requires failure notification for every published workflow. The
   notification contains workflow name, execution id and error message only.
   Confirm saving of successful/error/manual execution data is off.
5. Test once with **Test workflow** and a test request sent to the owner's own
   address (DEV is not a sandbox: every test sends a real e-mail). Then
   **Publish** (top right). Publish only when the run finishes without errors.
6. The production URL is `https://n8n.dev.gcp.alza.cz/webhook/ciz01-mailer`
   (already in `container/config.env`). `/webhook-test/...` works only while the
   editor listens for a test run. `/webhook` paths are public (not behind IAP),
   which is why Header Auth is mandatory.

Successful response must be JSON `{ "status": "sent", "requestId": "<the input
UUID>" }`, returned after the Outlook send completes. HTTP 200 alone does not
count as success. The app reports acknowledgement, not inbox receipt. Confirm
real receipt in the recipient mailbox. Local `VIBE_LOCAL=1` never calls n8n.

Payload:

```json
{"requestId":"UUID","to":"recipient@example.com","subject":"Subject",
 "body":"Plain text","requestedBy":"authenticated-user@alza.cz"}
```

The validator accepts exactly one recipient (no `,` or `;` lists), a one-line
subject up to 200 characters and a body up to 10 000 characters.

The app reserves send identifiers transactionally before invoking the webhook.
The same identifier will not call n8n twice, including after timeout or
ambiguous failure. If Outlook sends but the acknowledgement is lost, check the
owner's Outlook Sent Items before creating a new send attempt (executions are
not saved). Do not configure automatic retries around the non-idempotent send
node; the workflow does not deduplicate IDs itself.

No email content is persisted by the application itself; the template does not
save execution data. Confirm instance-level retention in the actual n8n host.

Sources: internal Alza n8n guide (instances, access, credentials, publishing,
error handling); n8n docs —
[Webhook](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/),
[Header Auth](https://docs.n8n.io/integrations/builtin/credentials/webhook/),
[Microsoft Outlook](https://docs.n8n.io/integrations/builtin/app-nodes/n8n-nodes-base.microsoftoutlook/),
[Respond to Webhook](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.respondtowebhook/),
[Error Trigger](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.errortrigger/).
