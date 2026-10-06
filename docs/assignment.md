# AI Academy assignment

## Sources and priority

The user's message and assignment screenshot define the application requirements.
`install.md` describes the company platform setup. It is setup reference material,
not permission to replace the application requirements. Platform coding context
must be fetched before selecting or scaffolding the application stack.

## Application scope

A test application for the AI Builder certification. A user provides instructions
for an email, AI drafts it, and an explicit send action submits it to an n8n
webhook. The workflow sends the email from the application owner's address.
Do not access production business data.

## Required capabilities from the screenshot

1. A company-provisioned GCP / Vibe project, requested through Service Desk.
   Verify project availability through `list_my_vibe_projects`.
2. An admin panel supporting management of application administrators and
   application access, following the platform's identity and storage rules.
3. Email delivery through n8n. Authenticate the webhook using Header Auth.
4. A meaningful AI capability using the Vertex AI / Agent Platform API.
5. Secret Manager integration, preferably for the n8n authentication secret.
   Never expose the secret in the browser, repository, or logs.
6. Read the provided BigQuery test dataset. This is an explicit assignment
   requirement despite the user's statement that the app does not touch data.
   Limit access to the academy test dataset; clarify any intended exemption
   before marking this requirement complete.
7. Use Spec Kit for specification-driven development. This assignment
   requirement overrides the setup guide's default of leaving Spec Kit off.

## Platform setup state

- Workspace was empty when inspected.
- Git 2.55.0 and Node.js 24.21.0 are available.
- Python 3.12.15 and Google Cloud CLI 587.0.0 were installed and verified.
- Visual Studio Code is already installed in `/Applications`.
- Project name, Google sign-in, MCP connection, platform context, n8n workflow,
  and BigQuery test dataset identifiers have not yet been verified.
- No application has been created or deployed.

## Completion evidence

Record actual local test results, successful cloud integration checks, deployment
commit and URL, and a mapping of all seven capabilities to their demonstrated
behavior. A simulated local response does not demonstrate a cloud integration.
