# US-001 — Patient file: view page

**Epic:** MVP Sprint 1 — Ship to doctor friends
**Roadmap item:** #1 Patient file (Add / search / **view** patient)
**Status:** Draft — pending architecture review
**Author:** Functional analyst
**Date:** 2026-06-12

---

## Context — what already exists

This story is scoped to the **missing piece only**. The rest of the "Patient file"
roadmap item is already implemented and is treated as given context, not work:

| Capability | State | Where |
|------------|-------|-------|
| Add patient | ✅ Built | `(app)/patients/new/+page.svelte`, `CreatePatientCommand` |
| Search / list patients | ✅ Built | `(app)/patients/+page.svelte`, `GetPatientsQuery` (term + paging), `api/patients/search` |
| Get single patient (API) | ✅ Built | `GetPatientByIdQuery`, `api/patients/[id]` proxy |
| Consultations of a patient (API) | ✅ Built | `api/patients/[id]/consultations` |
| Invoices of a patient (API) | ✅ Built | `api/patients/[id]/invoices` |
| **View a patient file (UI page)** | ❌ **Missing** | no `(app)/patients/[id]` route exists |

**The gap:** a doctor can add and find a patient, but cannot open a patient and see
who they are and their history on one screen. The data and proxy endpoints exist;
there is no detail page consuming them.

---

## Story

> **As a** doctor or receptionist in a cabinet,
> **I want to** open a patient from the list and see their identity, contact details,
> and visit history on one page,
> **so that** I know who I'm dealing with before a consultation without piecing it
> together from separate screens.

---

## Roles & access

- **Doctor** — full view.
- **Receptionist** — full view (front-desk staff need contact + visit info).
- Both are scoped to **their own cabinet** (`cabinetId` from the JWT claim — never from
  the URL). A patient belonging to another cabinet is treated as non-existent (see AC-7).
- Unauthenticated users hit the `(app)` layout guard and are redirected to `/login`.

---

## Scope

**In scope**
- A new route `(app)/patients/[id]` that renders a single patient file.
- An at-a-glance header (identity + contact + quick facts).
- A **consultations** history section (date, diagnosis/summary).
- An **invoices / payments** history section (date, amount, status).
- **Last visit** summary surfaced in the header.
- Empty / loading / error / not-found states for the page and each history section.
- A way to reach this page: clicking a patient row on the existing list page.

**Out of scope (explicitly deferred)**
- Editing patient fields from this page (view-only). → future story.
- Archiving / deactivating a patient. → future story.
- Surfacing or editing medical fields (allergies, blood group, medical history,
  current treatment). The data exists on the `Patient` aggregate but managing it is a
  separate clinical story. → future story.
- Creating a consultation or invoice from this page (links may navigate to existing
  flows, but the creation flows themselves are other stories).

---

## Data displayed

Sourced from `GetPatientByIdQuery` / `api/patients/[id]` and the two history endpoints.
**No new persisted fields.** All fields below already exist on the `Patient` aggregate.

**Header (at-a-glance)**
- Full name (`FirstName` + `LastName`)
- Age (derived `Patient.Age`) and gender (`M` / `F`)
- Phone (primary), email (if present)
- Last visit date (`LastVisitAt`) — or "Aucune visite" if null

**Identity / contact panel**
- Date of birth, address, wilaya (if present)
- NSS / insurance provider (display only, if present)
- Emergency contact name + phone (if present)

**Consultations section** — list, most recent first
- **Doctor:** date + diagnosis/summary; row links to the existing consultation detail flow.
- **Receptionist:** date + "Consultation effectuée" only — **no** diagnosis, reason,
  tariff, or prescription content (clinical fields are doctor-only). See ADR-002.

**Invoices / payments section** — list, most recent first
- Date, amount, payment status/method.

---

## Acceptance criteria

- **AC-1 — Open from list:** Clicking a patient row on `(app)/patients` navigates to
  `(app)/patients/[id]` for that patient.
- **AC-2 — Header renders:** The page shows the patient's full name, age, gender, phone,
  and last-visit date in a header, loaded server-side via the existing proxy.
- **AC-3 — Identity panel:** Date of birth, address/wilaya, and emergency contact are
  shown when present; absent optional fields are hidden or shown as "—" (never blank
  labels with broken layout).
- **AC-4 — Consultations history (role-shaped, see ADR-002):** Past consultations are
  listed most-recent-first. For a **Doctor**, each row shows date + diagnosis/summary and
  links to the consultation detail flow. For a **Receptionist**, each row shows date +
  "Consultation effectuée" only — no diagnosis, reason, tariff, or prescription content.
  With zero consultations, an empty state ("Aucune consultation") is shown for both — not
  an error.
- **AC-5 — Invoices history:** Invoices/payments are listed most-recent-first with date,
  amount, and status. With zero invoices, an empty state is shown.
- **AC-6 — Loading state:** While the page data loads, the user sees a loading indicator
  (skeleton or spinner), not a blank screen or a flash of "not found".
- **AC-7 — Not found / cross-cabinet (no leak):** Requesting an id that does not exist,
  or that belongs to another cabinet, renders a clean "Patient introuvable" 404 state.
  The two cases are **indistinguishable** — the page never reveals that a patient exists
  in another cabinet. (Backend already returns `KeyNotFoundException` → 404 for
  cross-cabinet loads.)
- **AC-8 — Error state:** If the backend call fails (non-404, e.g. 500/timeout), the page
  shows a recoverable error message ("Erreur lors du chargement du dossier patient")
  with a way to retry/return to the list — never a raw stack trace or silent blank.
- **AC-9 — Access by role:** A logged-in Doctor and a logged-in Receptionist of the
  patient's cabinet can both open the page. The **consultations section content differs by
  role** (metadata-only for receptionists, per AC-4 / ADR-002) — it is not hidden and does
  not error for receptionists. An unauthenticated user is redirected to `/login` by the
  `(app)` guard.
- **AC-10 — Cabinet scoping:** The patient id in the URL is **never** trusted for tenancy;
  the backend resolves the patient with `cabinetId` from the JWT. A tampered URL pointing
  at another cabinet's id behaves exactly like AC-7.
- **AC-11 — History section isolation:** If a history section's endpoint fails while the
  patient core loads fine, that section shows its own inline error/empty state; the rest
  of the page still renders (one failing list does not blank the whole file).

---

## Non-functional / conventions

- All backend calls go through `src/lib/server/api.ts` → `remoteApiProxy.ts`
  (API key + timestamp + JWT). No direct `fetch` to the backend.
- French UI copy, consistent with the rest of the app.
- Styling via existing tokens/utilities in `app.css` (`.card`, `.mk-table`, etc.).
- Page data loaded in `+page.server.ts` (server-only), passed to `+page.svelte` (Svelte 5
  runes: `$props()`).
- Audit: read access follows the existing audit conventions; no new write paths are
  introduced by this story.

---

## Open questions / assumptions

- **Assumption:** the consultation row link targets the already-built consultation detail
  flow; if that flow expects to be reached differently, the link target is adjusted in
  implementation (does not change this story's ACs).
- **Assumption:** "amount" formatting uses DZD as elsewhere in the finance module.

---

## Success signal

A doctor opens the patient list, clicks a patient, and immediately sees who they are,
how to reach them, when they last came, what was done, and what they owe/paid — on one
screen, with sensible states when something is empty, loading, missing, or broken.
