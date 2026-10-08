# AI Academy assignment

## Sources and priority

The user's message and assignment screenshot define the application requirements.
The owner explicitly designated root `install.md` as canonical alongside
`tech.md` and `principles.md`. Apply its explicit replacements of older setup
rules; retain the technical and coding rules it does not replace. These setup
rules coexist with the user's application requirements. Refresh server-managed
platform context through MCP; never hand-edit it. The unchanged archival copy
is at `docs/install.md`. Root placement is the owner's explicit preference,
which overrides install section 9's instruction to move the guide into docs.

Spec Kit was explicitly required in the supplied assignment, so it is enabled
locally despite being off by default in the setup guide. The owner also
explicitly authorized a private GitHub backup, overriding the default ban on
another remote; it does not replace platform deployment. GitHub upload is still
pending sign-in and has not been reported as completed.

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
- Google sign-in succeeded; the platform client is registered with Codex and
  returned a project list containing only the unrelated `shub` project.
- Generic Firestore platform context 4863456b58c9 was fetched and committed.
- Assignment title is `ciz01`. Owner-approved short platform
  name is `ciz01`. The owner reports the project request is now in approval;
  platform assignment and availability remain pending.
- Local application is implemented. Real n8n, Vertex, Secret Manager,
  BigQuery, Firestore, and cloud deployment acceptance remain pending.
- See `docs/verification.md` for actual local evidence and external blockers.

## Completion evidence

Record actual local test results, successful cloud integration checks, deployment
commit and URL, and a mapping of all seven capabilities to their demonstrated
behavior. A simulated local response does not demonstrate a cloud integration.

## 2026-10-08 access requirement and project update

Owner requires approved individual corporate addresses after requesting access.
Self-service request/status and admin approval/rejection are implemented; the
whole-company app mode is disabled. IAP remains the platform authentication
layer, and initial deployment must be owner-only until bootstrap. See access.md.
Owner reports project approval and will provide final assigned name after local
preparation. That report is not a substitute for MCP project-list verification.

Owner subsequently supplied approved request name prj-gcp-l-plat-gencode-ciz01
and confirmed corporate account martin.cizkovsky@alza.cz. Sign-in/ADC renewed;
platform list still contains only shub. Actual ciz01 registration and user
ownership/access remain to be verified by AI Enablement.
