# Attribution

The following directories are **vendored verbatim** (unmodified) from
[Addy Osmani's Agent Skills](https://github.com/addyosmani/agent-skills):

- `skills/`            — the 23 platform-agnostic engineering skills
- `references/`        — checklists referenced by the skills
- `agents/`            — subagent definitions used by the skills

The platform-specific wrappers (`.claude/`, `.gemini/`, `.opencode/`) from the
upstream repo are intentionally **not** vendored. The skills are driven
platform-agnostically via [`AGENTS.md`](./AGENTS.md) instead.

**Source:** https://github.com/addyosmani/agent-skills
**Commit:** `6ce029897d2b794940325fc7148774a6ec51111c`
**License:** MIT — see [`LICENSE-agent-skills`](./LICENSE-agent-skills)

These files are used as the engineering workflow for this repository and are
kept unchanged. The article that motivated their use:
https://addyosmani.com/blog/agent-skills/
