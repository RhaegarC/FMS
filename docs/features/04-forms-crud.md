# 04 — Forms CRUD

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — Decisions #10–#13/#15/#16 + Form schema model + data-model `forms`.

## Summary

The admin-facing forms API: create/read/update/list (and delete) a form definition inside a
space. Each form stores a mutable single `schema` (jsonb, draft 2020-12). Saving validates that
the schema is a legal JSON Schema — standard keywords only (`if`/`then`/`else` allowed), with
`x-dataSource` as the sole custom keyword.

## Story

As an admin I want to store and edit a form's JSON Schema, so that users can later fill it while a
bad schema is rejected at save time rather than at fill time.

## Dependencies

- [02-entra-auth](02-entra-auth.md) (the `admin` role gates configuration)

## Acceptance criteria

- [ ] Admin (`role = admin`) can create/update/get/list a form under a space; non-admin gets 403 on write
- [ ] Form = `id` (uuid), `spaceId`, `name`, `schema` (jsonb), `lastModifiedOn`; mutable single definition (no versions)
- [ ] Save validates the schema: valid draft 2020-12; standard keywords accepted incl. `if`/`then`/`else`; unknown custom keywords rejected except `x-dataSource`
- [ ] Schema updates follow the PRD schema-change discipline (PRD data-model table updated in the same PR)

## Tests (TDD)

- Unit (`Fms.Service.Test`) **hot spot**: schema validator — valid/invalid draft 2020-12, `if`/`then`/`else`, unknown-keyword rejection.
- Integration (`Fms.Api.Test` + `Fms.Repository.Test`): CRUD lifecycle; 403 for non-admin writes.

## Notes / non-goals

- JSON editor + live-preview UI are Figma-authored; the API only stores/validates schemas.
- Renderer behaviour (layout inference, conditional visibility, cascades) is client-side, not this API feature.
