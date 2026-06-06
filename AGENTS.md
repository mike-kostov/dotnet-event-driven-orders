# AGENTS.md

Guidance for AI coding agents (and humans) working in this repository.
This file is the **router**: it decides which workflow applies and enforces the
operating rules. The workflows themselves live in [`skills/`](./skills).

This project does not rely on slash commands or any platform-specific plugin.
Skills are followed by **reading `skills/<skill-name>/SKILL.md` directly** and
applying the workflow exactly (do not partially apply a skill).

---

## The five non-negotiables

These apply at all times, across every task. They are not optional.

1. **Surface assumptions before building.** State assumptions explicitly and
   invite correction. Wrong assumptions held silently are the most common
   failure mode.
2. **Stop and ask when requirements conflict.** Do not guess through ambiguity.
3. **Push back when warranted.** Not a yes-machine — challenge weak ideas.
4. **Prefer the boring, obvious solution.** Cleverness is expensive.
5. **Touch only what you're asked to touch.** No drive-by refactors, no
   rewriting code you don't fully understand, no scope you weren't given.

Verification is the load-bearing exit criterion: a task is done when there is
**evidence** it works (a green test run, a clean build, a runtime trace, a
review sign-off). "Seems right" never closes the loop.

---

## How we work: lifecycle → skill

Map intent to the corresponding skill and follow it. The meta-skill
[`using-agent-skills`](./skills/using-agent-skills/SKILL.md) is the full router;
this is the short version.

| Phase | When | Skill |
|-------|------|-------|
| DEFINE | Deciding what to build | `spec-driven-development` → writes `SPEC.md` |
| PLAN | Breaking work into atomic tasks | `planning-and-task-breakdown` |
| BUILD | Implementing | `incremental-implementation` + `test-driven-development` |
| VERIFY | Proving it works | `test-driven-development` |
| REVIEW | Before merge | `code-review-and-quality` |
| SIMPLIFY | Reducing complexity | `code-simplification` |
| SHIP | Releasing | `shipping-and-launch` |

Cross-cutting, activate as the situation calls for them:

- API / message contract design → `api-and-interface-design`
- Something broke → `debugging-and-error-recovery`
- Security-sensitive change → `security-and-hardening`
- Performance concern → `performance-optimization`
- Writing docs or ADRs → `documentation-and-adrs`
- Commits / branching → `git-workflow-and-versioning`
- High stakes / unfamiliar code → `doubt-driven-development`

A small change uses a few skills; a complex feature chains many. Scale the
workflow to the actual scope, not the assumed scope.

---

## Project specifics

What this project is, its acceptance criteria, structure, and detailed
boundaries live in **`SPEC.md`** (written via `spec-driven-development`).
Architecture decisions are recorded as ADRs under **`docs/adr/`**
(via `documentation-and-adrs`). Read those before implementing.

---

## Docs structure (this repo)

This is a **tutorial repo**: one coherent system, taught in incremental lessons
on `lesson/NN-*` branches. Documentation is split by *kind*, never dumped into a
single file. Write each artifact in its own place:

| Artifact | Location | Granularity |
|---|---|---|
| System spec (the product on `main`) | `SPEC.md` | **one** — six core areas, set once |
| Lesson plans (the incremental build path) | `docs/plans/NN-lesson.md` | **one per lesson** |
| Architecture decisions | `docs/adr/NNNN-*.md` | **one per decision** |
| System topology overview | `docs/architecture.md` | **one** |
| Idea one-pagers (idea-refine output) | `docs/ideas/*.md` | per idea |
| Learner-facing teaching content | `LESSON.md` in each `lesson/NN-*` branch | per lesson |

**Rules:**
- Do **not** collapse spec + plan + ADRs + architecture into one dump.
- Do **not** split `SPEC.md` per-lesson — lessons are *pedagogical slices* of one
  system, not subsystem boundaries. The system is specced once; the *teaching*
  of it is planned per-lesson.
- A lesson does not get its own six-area spec (tech stack, commands, structure,
  style, testing, boundaries are global). A lesson gets a **plan**: objective,
  the slice it implements, acceptance criteria, verification, and the "why".

---

## Provenance

The `skills/`, `references/`, and `agents/` directories are vendored verbatim
from [addyosmani/agent-skills](https://github.com/addyosmani/agent-skills)
(MIT). See [`ATTRIBUTION.md`](./ATTRIBUTION.md). They are kept unchanged.
