# Coding Principles

These principles govern how code is written on the Vibe Coding platform. They
apply to all applications regardless of archetype or tech stack. (If the user
opts into speckit, this file is the input for its constitution; speckit is
otherwise not used.)

## I. No Secrets in Code

Never hardcode secrets, API keys, credentials, or sensitive configuration in
source code. Secrets live in Secret Manager (`create_secret`) and reach the
application as environment variables — never in the repository, never in
`config.env`, never in Firestore. If a secret is accidentally committed, treat
it as compromised and rotate it immediately.

## II. Reusable Code

Do not duplicate logic. Extract shared behavior into functions, utilities, or
modules. Before writing new code, check if similar functionality already
exists. Prefer composing small, focused functions over monolithic blocks. When
a pattern appears three or more times, abstract it.

## III. Simplicity First

Write the simplest code that solves the problem. Avoid premature abstraction,
over-engineering, and speculative generality. Solve what is needed now. Prefer
explicit code over clever code.

## IV. Fail Fast and Loud

Detect errors early and report them clearly. Validate inputs at system
boundaries. If required configuration is missing, crash at startup with a
descriptive error — never silently fall back to defaults that mask problems.
Never swallow exceptions silently.

## V. Clear Naming

Names must communicate intent. Avoid abbreviations, single-letter variables
(except loop counters), and generic names like `data`, `result`, or `handler`
without context. Use the same term for the same concept throughout.

## VI. Small, Focused Units

Functions do one thing. Modules have a single responsibility. Keep files short
enough to understand without extensive scrolling. Prefer many small files over
few large ones.

## VII. Defensive at Boundaries, Trusting Inside

Validate rigorously at system boundaries: user input, request bodies, external
service responses, environment variables. Once validated, trust the data
internally. Use strongly-typed models with validation at entry points.

## VIII. Minimal Dependencies

Only add dependencies that provide clear, significant value. Prefer the
standard library or a few lines of code. Pin dependency versions.

## IX. Test-Driven Development

Write tests before writing code, and make them fail before making them pass.
Cover the main paths and the edge cases that matter. Tests run locally before
every push; nothing is pushed red.

## X. Verify the Deployment

A change is done when it runs on the platform, not when it compiles. After
every push, watch CI and CD to completion (`get_deploy_status`), confirm the
result, and report it to the user in plain language.

## XI. Access Control Is the App's Job

IAP in front of the app authenticates colleagues and may admit the whole
organisation or only a list (`list_app_access`, `grant_app_access`); the owner
always keeps IAP access. That gate does not decide who may use *this* app:
every app ships with an admin panel and an access model (who is admin, who may
use the app). The first visitor becomes admin so the app is usable immediately
after the first deploy. Compare emails case-insensitively.
