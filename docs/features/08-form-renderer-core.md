# 08 — Form renderer core

Status: **Not started** (spec drafted 2026-08-06)

## Summary

`packages/form-renderer`: the shared engine that renders any standard JSON Schema form — schema→widget mapping, layout inference (field order = schema order, widget by type), ajv validation, and value state. Used by both portals (admin preview + user fill).

## Dependencies

- 01-openapi-contract.md (generated schema types)

## User story

As a developer, I want one shared renderer so that admin previews and user forms are the same code path.

## Acceptance criteria

- [ ] Field types rendered: `string` (text), `string`+`enum` (select/radio), `string`+`format:textarea`, `string`+`format:date`, `number`/`integer`, `boolean`
- [ ] Layout inferred from schema property order
- [ ] Validation via ajv against the schema
- [ ] Value state managed centrally; invalid values flagged
- [ ] Extensible widget registry (custom widgets register per type/format)

## Tasks

- [ ] Widget registry + default widget mapping
- [ ] Layout inference
- [ ] ajv validation integration
- [ ] Value/state model
