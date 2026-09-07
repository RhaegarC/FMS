# 03 — Permission Evaluation

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — Decisions #20/#21 + "Authorization for data access" + data-model `permissions`.

## Summary

The access-control core. A `permissions` table stores SQL-predicate expressions per space/form;
an evaluation service tests them against the caller's `user.id`/`email`/`role` at request time.
A `space` grant implies its forms; effective access = union of space + form grants; **default
deny**. Enforced on every data read in features 05–07.

## Story

As a space owner I want to grant a space or a single form to specific users via a SQL row — no UI —
so that access is auditable and defaults to closed.

## Dependencies

- [02-entra-auth](02-entra-auth.md) (caller identity + role)

## Acceptance criteria

- [ ] `permissions` rows: `resource_type` (`space`|`form`), `resource_id` (`*`|uuid-as-text), `expression` (text)
- [ ] A grant whose expression is true for the caller grants access; **no matching true row → deny**
- [ ] A `space` grant covers every form under that space; effective access = union of space + form grants
- [ ] Expressions may reference only `user.id`, `user.email`, `user.role`
- [ ] Evaluation is invoked (and denies by default) whenever forms/submissions are read

## Tests (TDD)

- Unit (`Fms.Service.Test`) **hot spot (security)**: expression truth over caller attrs; space→forms implication; union; default deny.
- Integration (`Fms.Repository.Test` + `Fms.Service.Test`): seeded rows → expected allow/deny matrix.

## Notes / non-goals

- Expression is admin-written, trusted input (like a SQL view); no expression web UI.
- The **config** capability is role-based (`admin`, via 02); expressions gate view/fill of spaces/forms.
