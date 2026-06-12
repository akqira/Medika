# ADR-001 — Redact PII and secrets in HTTP detail logging

**Status:** Proposed
**Date:** 2026-06-12
**Deciders:** Solution architect (pending sign-off by project owner)
**Context trigger:** Architecture review of US-001 (Patient file — view page)

---

## Context

`Medika.Api/Middleware/HttpDetailLoggingMiddleware.cs` (added in commits `bc256f0` /
`028c40a`) captures, for **every** non-Swagger request:

- All **request headers**, including `Authorization: Bearer <JWT>` and `X-API-KEY`
  (the middleware's own doc-comment states it captures "headers (incl. Authorization
  bearer)").
- All **response headers**.
- The full **request body** and full **response body** (textual content types, truncated
  at 8000 chars).

These are written to **three** sinks: Application Insights `RequestTelemetry` custom
properties, Serilog console, and a rolling file (plus the App Insights traces sink).

US-001 makes `GET /api/patients/{id}` a primary, high-frequency path: doctors open
patient files all day. That endpoint's response body contains **patient PII** —
full name, date of birth, phone, email, address, wilaya, **NSS (numéro de sécurité
sociale)**, insurance provider, emergency contact, allergies, and medical history.
The consultations and invoices history endpoints add diagnoses and financial data.

**Consequence today:** opening a patient file writes that patient's complete PII —
including their social-security number and medical history — into Application Insights
and a log file, alongside the bearer token and API key used to make the call. This is a
standing data-protection violation for health data, and US-001 turns a rarely-hit code
path into an everyday one.

This blocks the project security checklist items **"Is PII excluded from logs?"** and
**"Are secrets in env vars / Key Vault (never in code)?"** (secrets must also never be in
logs).

## Decision

1. **Never log secrets.** Redact `Authorization`, `X-API-KEY` (and `Cookie`/`Set-Cookie`)
   header values in `FormatHeaders` — emit `Authorization: [redacted]`. This is
   unconditional, all environments.

2. **Do not log request/response bodies for sensitive routes.** Maintain a deny-list of
   path prefixes whose bodies are replaced with `<redacted: NNN bytes>`:
   `/api/patients`, `/api/consultations` (and `/api/patients/{id}/consultations`),
   `/api/finance` / invoices, `/api/auth` (login carries credentials). Headers may still
   be logged (post-redaction) for these routes; bodies may not.

3. **Bodies-on by default only in Development.** Default `Logging:CaptureHttpDetails` to
   the body-capturing behaviour **only** when `ASPNETCORE_ENVIRONMENT=Development`. In
   Staging/Production the middleware logs method/path/status/duration and redacted
   headers, but **not** request/response bodies, regardless of route — bodies are opt-in
   per route for short-lived debugging only.

4. **Telemetry retention.** Confirm App Insights retention/sampling does not silently
   persist any pre-fix logged PII beyond the data-retention policy; purge if it already
   has.

## Consequences

- **Positive:** Patient PII (notably NSS and medical history) and auth secrets stop
  flowing to telemetry and log files. US-001 can ship its view page without each file-open
  becoming a PII spill. Defensible posture for Algerian health-data handling.
- **Negative:** Less verbose production logs for backend debugging of patient/finance
  endpoints; engineers must reproduce in Development (where bodies are still captured) or
  temporarily opt a route in. Acceptable trade-off — diagnosability does not outrank
  patient confidentiality.
- **Neutral:** No change to the middleware's ordering (still before `ApiKeyMiddleware`) or
  to non-sensitive routes like `/api/health`.

## Alternatives considered

- **Hash/mask individual PII fields in bodies** — rejected: brittle (every new field is a
  new leak), couples logging to DTO shape, and still risks partial exposure. Route-level
  body suppression is simpler and fails safe.
- **Disable the middleware entirely** — rejected: it has real value for non-sensitive
  request tracing and 401 diagnosis; the problem is scope, not existence.

## Scope note

This ADR is **not** new work created by US-001 — it is a pre-existing condition that
US-001 surfaces and amplifies. It is tracked as a **blocking condition** on US-001
reaching production, not on the frontend implementation work itself.
