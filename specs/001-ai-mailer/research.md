# Research

## Platform contract

Decision: C# .NET 10 + React, one container, ADC, IAP header identity.
Rationale: mandated by fetched platform context 4863456b58c9.
Alternatives: Python is reserved for existing apps; no other hosting provider.

## Access and delivery coordination

Decision: Firestore transactions serialized through `settings/access` for admin
changes and a per-request document for delivery reservations. Memory equivalent
is locked. Never delete the final admin. Bodies/prompts are not stored.
Rationale: race-free bootstrap and at-most-one webhook invocation per identifier.
Alternative: process-local coordination would fail with multiple Cloud Run instances.
[Firestore transactions](https://docs.cloud.google.com/dotnet/docs/reference/Google.Cloud.Firestore/latest/Google.Cloud.Firestore.Transaction).

## AI and test data

Decision: configurable Vertex model, region europe-west4, ADC-authenticated REST,
structured JSON subject/body, defensive output checks. BigQuery table metadata
plus tabledata.list capped at 20 rows; no query execution or write API.
Rationale: simple APIs with least data scope; no API key or arbitrary SQL.
[Structured output](https://docs.cloud.google.com/vertex-ai/generative-ai/docs/multimodal/control-generated-output).
[BigQuery table preview](https://docs.cloud.google.com/bigquery/docs/reference/rest/v2/tabledata/list).

## n8n

Decision: POST webhook with Header Auth, Microsoft Outlook send, Respond to Webhook
returning status=sent and the same requestId. No automatic retries. Owner controls
sender mailbox. Acknowledgement is not proof of inbox receipt.
Rationale: academy hint and documented webhook behavior. Generic HTTP 200 alone
is insufficient; the send may succeed before a timeout or later workflow failure.
Revision 2026-11-02: the internal Alza n8n guide makes the DEV instance
n8n.dev.gcp.alza.cz the working host and Microsoft credentials admin-managed, so
the originally planned SMTP node was replaced by the Outlook node.
[Webhook](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/).
[Respond node](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.respondtowebhook/).

Research performed with the Spec Kit research agent and primary documentation.
External identifiers remain deployment inputs, not design ambiguities.
