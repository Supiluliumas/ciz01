Read `AGENTS.md` — it is the single source of project instructions for every AI agent working here.

These context files (`AGENTS.md`, `CLAUDE.md`, `tech.md`, `principles.md`) are delivered by the vibe-git-mcp server: at session start call `get_vibe_context` with the version in `.vibe/context.lock` and refresh them if a newer version exists. Never hand-edit them. Speckit is a tool for you, not a requirement.
