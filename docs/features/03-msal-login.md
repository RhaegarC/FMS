# 03 — MSAL login in both portals

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Both React apps authenticate against Entra via MSAL, protect their routes, and acquire tokens for backend API calls.

## Dependencies

- 02-entra-auth.md

## User story

As a user, I want the admin and user portals to sign me in and keep me signed in, so that I can use the product without re-entering credentials.

## Acceptance criteria

- [ ] Admin portal login works (MSAL redirect flow)
- [ ] User portal login works
- [ ] Protected routes redirect to login when unauthenticated
- [ ] API calls carry a valid bearer token; expired tokens refresh or re-auth
- [ ] Admin config routes additionally require role `admin`

## Tasks

- [ ] MSAL config per app (clientId, tenant, redirect URI)
- [ ] Login/logout flow in both apps
- [ ] Route guards (auth + admin role)
- [ ] Token acquisition for API calls
