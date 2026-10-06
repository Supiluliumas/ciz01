# n8n setup

Import `n8n-workflow.json` in the academy's n8n instance. This is an unconfigured
workflow template; importing and validating it in the actual n8n instance is
still required. The agent has no connected n8n administration tool.

1. On Webhook, use POST, Header Auth, and response via Respond to Webhook.
   Create an HTTP Header Auth credential with name `X-N8N-Token` and a strong
   random value. Use the same value in GCP Secret Manager, mounted as
   `N8N_AUTH_TOKEN`. Never use Basic Auth or export credentials to this repo.
2. On Send Email, attach the owner's authorized SMTP credential. Replace
   `owner@example.com` with the owner's mailbox. Sender is fixed by the
   workflow, never derived from user input. Never enable continue-on-error.
3. Validate the Code node, publish/activate the workflow, and copy its
   **production** webhook URL to `N8N_WEBHOOK_URL` (non-secret configuration).
   A test URL works only while n8n listens for a test execution.
4. Successful response must be JSON `{ "status": "sent", "requestId":
   "<the input UUID>" }`, after the SMTP send completes. HTTP 200 alone does
   not count as success. The app reports acknowledgement, not inbox receipt.
5. Test via the configured app and confirm real receipt from the owner's
   mailbox. Local VIBE_LOCAL=1 intentionally never calls n8n.

Payload:

```json
{"requestId":"UUID","to":"recipient@example.com","subject":"Subject",
 "body":"Plain text","requestedBy":"authenticated-user@alza.cz"}
```

The app reserves send identifiers transactionally before invoking the webhook.
The same identifier will not call n8n twice, including after timeout or ambiguous
failure. If SMTP succeeds but acknowledgement is lost, check the workflow before
creating a new send attempt. Do not configure automatic retries around this
non-idempotent SMTP node; the workflow does not independently deduplicate IDs.

The template disables saved successful/error/manual execution bodies. Confirm
these settings and any instance-level retention policy in the actual n8n host.
No email content is persisted by the application itself.

Sources: [Webhook](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/),
[Header Auth](https://docs.n8n.io/integrations/builtin/credentials/webhook/),
[Send Email](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.sendemail/),
[Respond](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.respondtowebhook/).
