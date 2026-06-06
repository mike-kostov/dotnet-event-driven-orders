# ADR-0012: Lesson-branch tutorial structure

## Status
Accepted

## Date
2026-06-05

## Context
This repo is a tutorial, not just a system. Learners (new to backend, including
front-end developers) need to build it up incrementally rather than read a
finished codebase. We must choose a git structure that presents progressive steps
without drowning the learner in branch bookkeeping or content duplication.

## Decision
**`main` is the foundation; `lesson/NN-*` branches are cumulative completed
checkpoints; the final lesson branch is the finished system.**

- `main` — the **foundation only**: docs (`SPEC.md`, ADRs, plans, architecture),
  `AGENTS.md`, and the vendored `skills/`, `agents/`, `references/`. **No service
  code.** A fresh clone gives a learner the map and the starting line, not the
  destination.
- `lesson/01-...` … `lesson/10-...` — each branch holds the cumulative
  **completed** state through that lesson (lessons 1..N done). Every branch builds
  and runs.
- `lesson/10-...` — the **finished system** the rest of `SPEC.md` specifies. This
  is the reachable end state: one `git checkout lesson/10-...`.
- Each branch carries a **`LESSON.md`** with the conceptual *why*, objectives,
  steps, and verification for the work that branch introduces. The per-lesson
  development plan lives in `docs/plans/NN-lesson.md` (dev-facing); `LESSON.md` is
  learner-facing.

Two ways to learn:
- **Do it:** start from the previous branch (or `main` for lesson 1), follow the
  next `LESSON.md`, build toward it, and `git diff` against the lesson branch to
  check your work.
- **Read it:** walk `lesson/01 → lesson/10`, each a coherent, progressively
  complete, runnable checkpoint.

Lessons are **milestone-driven**, not pinned to a fixed count (~10 expected); a
lesson splits if it naturally does.

## Alternatives Considered

### Paired `lesson/NN-start` + `lesson/NN-solution` branches
- Pros: each lesson is self-contained; solution always present.
- Cons: `lesson/NN-solution` and `lesson/N+1-start` are the same tree — pure
  duplication, doubled branch count, and every fix must touch two branches.
- Rejected: the next lesson already *is* the solution; the duplication is waste.

### Git tags as checkpoints (no branches)
- Pros: clean linear history; minimal refs.
- Cons: tags are passive — awkward to attach evolving `LESSON.md` content to, and
  less intuitive for a beginner to "work on" than a branch.
- Rejected: branches carry teaching content more naturally.

### Single branch, lessons as docs only
- Pros: simplest repo.
- Cons: no way to check out and build a specific step; learner can't diff progress.
- Rejected: the checkout-and-build progression is the core of the experience.

### `main` as the finished system (portfolio-first)
- Pros: a fresh clone runs immediately; matches the common open-source expectation
  and the reference Go repo; best for showing the repo off as a finished piece.
- Cons: hands a beginner the destination on clone, undercutting the journey this
  tutorial is built around.
- Rejected: this repo is **tutorial-first**, not portfolio-first. (This supersedes
  the originally-drafted intent of this ADR, which had `main` as the finished system.)

## Consequences
- A fresh clone (`main`) has **no finished code** — the destination must be reached
  (or explicitly checked out), keeping the journey front-and-center.
- The finished system lives on `lesson/10-...`, reachable in one `git checkout`;
  `main`'s README **must** point clearly to both the end state and the lesson
  sequence (otherwise a casual visitor sees only docs).
- Every lesson branch must build and run (a success criterion in `SPEC.md`).
- A fix to early code must be propagated forward through later branches — the main
  maintenance cost, accepted as the price of clean, coherent checkpoints.
