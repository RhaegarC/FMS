# 11 — Read-only render mode

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Render mode that displays a saved submission read-only against its form's schema (disabled widgets, values shown). Used for submission detail views in both portals.

## Dependencies

- 08-form-renderer-core.md

## User story

As a user, I want to view a past submission as a readable form so that I can see exactly what I submitted.

## Acceptance criteria

- [ ] Renderer accepts a mode flag (fill / read-only)
- [ ] All widgets render disabled in read-only mode
- [ ] Lookup values render as their labels (from stored value or refetch)
- [ ] Hidden/unset fields render appropriately (skipped or "not answered")

## Tasks

- [ ] Mode flag through renderer
- [ ] Disabled widgets
- [ ] Lookup value display
