# 08 — Remote-Lookup Proxy

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — Decisions #15/#16 + the `x-dataSource` block in Form schema model.

## Summary

A backend endpoint that proxies remote lookups for `x-dataSource` dropdowns: given a URL with
`{fieldName}` placeholders and the current values, fetch server-side, map `valueField`/
`labelField`, and return normalized `[{value, label}]`. A URL allowlist policy (from `.env`)
refuses anything not permitted (anti-SSRF).

## Story

As an admin I want dropdown options to come from a remote API without exposing that API or my
credentials to the browser, so lookups stay server-side and safe.

## Dependencies

- [02-entra-auth](02-entra-auth.md) (only signed-in callers may use the proxy)

## Acceptance criteria

- [ ] The proxy fetches the configured URL server-side and returns `[{value, label}]` mapped from `valueField`/`labelField`
- [ ] `{fieldName}` placeholders are substituted from the caller-supplied current values (cascading input)
- [ ] The URL policy (allowlist, from `.env`) is enforced — disallowed URLs are refused
- [ ] Only authenticated callers can invoke the proxy

## Tests (TDD)

- Unit (`Fms.Service.Test`) **hot spot (security)**: allowlist enforcement; placeholder substitution.
- Integration (`Fms.Service.Test`/`Fms.Api.Test`): mocked upstream → normalized payload returned.

## Notes / non-goals

- The dropdown widget, refetch-on-change (cascading), and visibility ordering are client-side
  renderer behaviour (Figma + glue), not part of this API feature.
