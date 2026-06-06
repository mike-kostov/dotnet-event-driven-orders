# ADR-0014: Lesson scaffolding — skeletons with TODO markers

## Status
Accepted

## Date
2026-06-06

## Context
Lesson branches are START states (ADR-0012): the learner builds each lesson's slice
in place. A start state can range from a blank page (create everything yourself) to
fully-written (just read it). For an audience new to backend — including front-end
developers — the biggest friction is not *writing* code, it's **orientation**:
"where do I look, where do I start, what file do I add, what is its name, and where
does it go?" That uncertainty is the main source of drop-off.

## Decision
Every lesson branch ships a complete **skeleton**: the correct file tree, with
correct file names and locations already in place, and the spots the learner fills
marked with explicit `// TODO(you):` / `# TODO(you):` comments. Each TODO maps to a
numbered step in that lesson's `LESSON.md`. The learner fills blanks within a frame
— never faces an empty directory or guesses a path or a filename.

A lesson branch therefore provides:
- the directories and files that lesson introduces, named and placed exactly as the
  finished system expects;
- imports/usings, signatures, and structure around each gap, so the surrounding
  code compiles or is clearly marked as pending;
- `TODO(you)` markers at each spot to implement, cross-referenced to `LESSON.md`;
- everything that is *not* the point of this lesson, pre-written, so focus stays on
  the new concept.

## Alternatives Considered

### Blank page (learner creates all files)
- Pros: maximal practice; mirrors greenfield work.
- Cons: orientation overload — naming, placement, and project wiring drown the
  actual lesson; highest drop-off risk for beginners.
- Rejected: this is exactly the uncertainty we most want to remove.

### Fully-written (learner only reads / runs)
- Pros: zero friction; fastest to "it works".
- Cons: passive; little retention; not really *doing* the lesson.
- Rejected: the build-it-yourself loop is the point (ADR-0012).

## Consequences
- Authoring each lesson includes designing the skeleton + TODO set, not just the
  solution; the `docs/plans/NN-lesson.md` tasks describe both.
- `LESSON.md` steps and the in-code `TODO(you)` markers must stay in lockstep (same
  numbering), or the learner loses the thread.
- The diff from a lesson branch to `lesson/N+1` (or `final`) is exactly the
  filled-in TODOs — clean to check work against.
- Slightly more authoring effort per lesson, repaid by a frictionless learner path.
