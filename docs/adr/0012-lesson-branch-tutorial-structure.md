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
**`main` is the foundation; `lesson/NN-*` branches are per-lesson START states; a
`final` branch holds the finished system.**

- `main` — the **foundation only**: docs (`SPEC.md`, ADRs, plans, architecture),
  `AGENTS.md`, and the vendored `skills/`, `agents/`, `references/`. **No service
  code.** A fresh clone is the starting line and the map; its README points the
  learner to `lesson/01`.
- `lesson/01-...` … `lesson/10-...` — each branch is the **start state** of that
  lesson: the previous lessons' code **complete**, plus this lesson's `LESSON.md`
  (the walkthrough) and a complete file skeleton with `TODO(you)` markers to fill
  in (the scaffolding convention — ADR-0014). The learner builds this lesson's
  slice **in place** on the branch.
- Because each branch carries the prior lessons complete, **`lesson/N+1` is the
  solution to `lesson/N`** — no separate start/solution branch pair, no
  duplication. To check your work on lesson N, `git diff` against `lesson/N+1`
  (or `final` for the last lesson).
- `final` — all lessons complete: the **finished system** `SPEC.md` specifies and
  the reachable end state (`git checkout final`).
- Each lesson branch's `LESSON.md` is learner-facing (the conceptual *why*,
  objectives, steps, verification). The dev-facing plan lives in
  `docs/plans/NN-lesson.md`.

Two ways to learn:
- **Do it:** check out `lesson/NN`, follow its `LESSON.md`, build the slice in
  place, then `git diff lesson/N+1` (or `final`) to check your work.
- **Read it:** walk `lesson/01 → … → lesson/10 → final`; each branch shows prior
  lessons complete with the current lesson laid out to build.

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
  and the reference Go repo; best for showing the repo off as a finished piece;
  no extra `final` branch needed (the solution to the last lesson is `main`).
- Cons: hands a beginner the destination on clone, undercutting the journey this
  tutorial is built around.
- Rejected: this repo is **tutorial-first**, not portfolio-first.

## Consequences
- A fresh clone (`main`) has **no service code** — the learner starts at
  `lesson/01`; the destination (`final`) is reachable but not handed over.
- `main`'s README **must** point clearly to `lesson/01` (start here) and `final`
  (the end state), or a casual visitor sees only docs.
- Each lesson branch must build and run: the prior lessons' features work, and the
  current lesson's stubs compile (a success criterion in `SPEC.md`).
- The "solution" to a lesson is simply the next branch (`lesson/N+1`, or `final`
  for the last) — a `git diff` shows exactly what the lesson adds.
- A fix to early code must be propagated forward through all later branches **and**
  `final` — the main maintenance cost, accepted as the price of clean checkpoints.

> Model history: this ADR was first drafted with `main` as the finished system,
> then briefly with lesson branches as *completed* states. The settled model is
> the above — start-state lesson branches + a `final` branch.
