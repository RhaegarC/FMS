# 01 — Capture the spec and the standard

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: — (this stage unblocks every other stage)
Branch: `refactor/01-capture-spec-and-standard`

## Summary

Make the written standard and the spec agree before any code moves. Today `src/api`
*executes* one standard while `docs/backend-standard.md` documents another — the template's
rules (keys, delete semantics, audit, config keys) contradict it. This stage replaces the
document so the remaining seven stages have something to implement against.

This stage changes **no code under `src/api/`**.

## Scope

| Path | Action |
|---|---|
| `src/api/STANDARD.md` | new — adapted from the template's `src/content/STANDARD.md` |
| `docs/features/09-backend-template-alignment.md` | new — via `/capture`, in the repo's feature shape |
| `docs/backend-standard.md` | **delete** |
| [.claude/CLAUDE.md](../../.claude/CLAUDE.md) | repoint the `backend-standard.md` link at `src/api/STANDARD.md` |
| [docs/PRD.md](../PRD.md) | repoint §6.1.1 references; fix the now-wrong key-generation wording |
| [docs/testing-and-tdd.md](../testing-and-tdd.md) | repoint its `backend-standard` references |
| `src/api/**` | strip/redirect the `backend-standard §…` comments (~15 files) |
| `.claude/skills/gitnexus-*`, CLAUDE.md gitnexus block | adopt from `BackendTemplate` |

## Acceptance criteria

- [ ] `src/api/STANDARD.md` exists; `Tmp.*` → `Fms.*`, `TmpContext` → `FmsDbContext`, `Template`→ FMS paths
- [ ] Its §1 layout table matches the real `Fms.slnx` (five app projects + **three** test projects)
- [ ] Its §12 lists this repo's genuine gaps, and carries the **eight deviations** from the parent plan's
      *"Deviations the template cannot express"* section as first-class rules
- [ ] `docs/backend-standard.md` deleted; `grep -rn "backend-standard"` across the repo returns only
      intentional historical references (feature archives, this plan)
- [ ] `docs/features/09-backend-template-alignment.md` exists in the `/capture` shape (status, summary,
      dependencies, story/scope, acceptance criteria, tests, non-goals)
- [ ] The gitnexus skill files and the `<!-- gitnexus:start -->` CLAUDE.md block are present, and
      `gitnexus status` reports the repo up-to-date

## Tests (TDD)

**No RED/GREEN cycle — this stage is documentation only.** The repo's TDD mandate covers backend
API behaviour, and no `src/api` code changes here. Verification is:

- `grep -rn "backend-standard" .` → no dangling links or stale rule citations
- `grep -rn "Tmp\.\|TmpContext\|BackendSolution" src/api/STANDARD.md` → no template placeholders left
- `dotnet test` (from `src/api/`) → still green, proving nothing was disturbed

## Traps

- ⚠️ **Do not delete `docs/backend-standard.md` until `src/api/STANDARD.md` exists.** The two documents
  disagree; during this stage the repo briefly holds both, and only one may be authoritative.
- ⚠️ The deviations section is the load-bearing part of `STANDARD.md`. A rule without its *reason*
  gets "simplified" away by the next person — the template's own preface says exactly this. Keep the
  reasoning for each of the eight.
- ⚠️ `docs/PRD.md` states keys are "generated via `gen_random_uuid()`". Stage 05 removes that default
  in favour of application-assigned keys. Fix the wording here, in the same change that introduces
  the new rule, so the PRD never contradicts the standard.

## Out of scope

- Any code change under `src/api/` — that starts at stage 02.
- Rewriting the template's rules to suit FMS. Where FMS must differ, it is a recorded deviation, not a
  silent edit.
