# Claude memory snapshot

A committed copy of the machine-local Claude Code auto-memory
(`~/.claude/projects/.../memory/`), kept in source so the full per-topic detail
travels across machines. It complements the repo-root **`CLAUDE.md`**, which is
the distilled context Claude Code auto-loads on any machine.

- `MEMORY.md` — index of the notes below.
- `project_*.md` — durable project facts/decisions.
- `reference_*.md` — pointers to specs, docs, walkthroughs, and how to run.
- `feedback_*.md` — working agreements / corrections.

These are point-in-time notes; if they drift from the code, the code and
`CLAUDE.md` win. Re-sync by copying the live memory dir over this folder.
