# TS-001 — Patient file: view page

**Covers:** US-001 — Patient file: view page
**Type:** Manual / acceptance scenarios (no automated test harness in the project yet)
**Date:** 2026-06-12

---

## Preconditions

- Backend (`https://localhost:5100`) and frontend (`https://localhost:5000`) running.
- Seeded doctor account exists (`kader.kebir@gmail.com` / `Doctor@123`) in cabinet A.
- At least:
  - **P1** — a patient in cabinet A with ≥ 2 consultations and ≥ 1 invoice, and a
    non-null last visit.
  - **P2** — a patient in cabinet A with **no** consultations and **no** invoices.
  - **P3** — a patient belonging to a **different** cabinet B (for the leak test).
- A Receptionist account in cabinet A (for the role test); if none exists, note it as
  blocked rather than skipping.

> Map the concrete ids before running: `P1_ID`, `P2_ID`, `P3_ID`.

---

## Scenarios

### TS-001.1 — Open a patient from the list (AC-1, AC-2) — happy path
1. Log in as the doctor; go to `(app)/patients`.
2. Click the row for **P1**.
- **Expect:** URL becomes `/patients/{P1_ID}`. Header shows P1's full name, age, gender,
  phone, and last-visit date.

### TS-001.2 — Identity / contact panel (AC-3)
1. On P1's page, inspect the identity panel.
- **Expect:** Date of birth, address/wilaya, and emergency contact render when present.
2. Open **P2** (assume P2 has no email/address/emergency contact).
- **Expect:** Absent optional fields are hidden or shown as "—"; no broken labels, no
  empty rows with dangling colons, layout intact.

### TS-001.3 — Consultations history populated (AC-4)
1. Open **P1**.
- **Expect:** Consultations listed **most-recent-first**, each with date and
  diagnosis/summary. Count matches the seeded consultations.
2. Click a consultation row.
- **Expect:** Navigates to the existing consultation detail flow for that consultation.

### TS-001.4 — Consultations empty state (AC-4)
1. Open **P2**.
- **Expect:** A friendly empty state ("Aucune consultation") in the consultations section
  — **not** an error, **not** a spinner stuck forever, **not** a blank gap.

### TS-001.5 — Invoices history populated (AC-5)
1. Open **P1**.
- **Expect:** Invoices listed most-recent-first with date, amount (DZD), and status/method.

### TS-001.6 — Invoices empty state (AC-5)
1. Open **P2**.
- **Expect:** Empty state in the invoices section; rest of page unaffected.

### TS-001.7 — Loading state (AC-6)
1. Throttle the network (DevTools → Slow 3G) and open **P1**.
- **Expect:** A loading indicator (skeleton/spinner) shows while data loads; no flash of
  "Patient introuvable", no blank white screen.

### TS-001.8 — Unknown id → not found, no leak (AC-7)
1. Navigate directly to `/patients/00000000-0000-0000-0000-000000000000`.
- **Expect:** Clean "Patient introuvable" 404 state with a way back to the list.

### TS-001.9 — Cross-cabinet id → identical not found (AC-7, AC-10) — security
1. Logged in as cabinet A doctor, navigate directly to `/patients/{P3_ID}` (P3 is in
   cabinet B).
- **Expect:** The **exact same** "Patient introuvable" state as TS-001.8 — same status,
  same copy, same timing as far as observable. No name, no hint, nothing that reveals P3
  exists. Confirm the backend returned 404 (not 403, not 200 with empty body).

### TS-001.10 — Backend error state (AC-8, AC-11)
1. Stop the backend (or block `api/patients/[id]`) and open **P1**.
- **Expect:** Recoverable error message ("Erreur lors du chargement du dossier patient")
  with retry/return-to-list — no raw stack trace, no silent blank.
2. Restart backend, but make **only** the invoices endpoint fail while patient + consultations load.
- **Expect:** Header and consultations render; the invoices section shows its **own**
  inline error/empty state. The whole page does **not** blank out (section isolation).

### TS-001.11 — Receptionist access, metadata-only consultations (AC-9, ADR-002) — security
1. Log in as the cabinet A **Receptionist**; open **P1** from the list.
- **Expect:** Page renders; header, identity/contact, and invoices show the same as for the
  doctor. No 403 banner on the consultations section.
2. Inspect the consultations section.
- **Expect:** Each consultation shows **date only** ("Consultation effectuée") — **no**
  diagnosis, reason, tariff, or prescription content. Confirm in the network response that
  the receptionist-safe payload does **not** contain `diagnosis`/`reason`/`tariff` fields
  (not merely hidden in the UI — absent from the data).
3. As the receptionist, attempt to open a consultation detail row.
- **Expect:** No clinical detail flow is reachable from the receptionist view (no link, or
  the detail endpoint denies). Diagnoses never reach front-desk staff.

### TS-001.12 — Unauthenticated redirect (AC-9)
1. Log out (clear `medika_token`). Navigate directly to `/patients/{P1_ID}`.
- **Expect:** Redirected to `/login` by the `(app)` layout guard; patient data never
  rendered.

### TS-001.13 — Proxy path enforced (non-functional)
1. Inspect the network calls made by the page.
- **Expect:** Patient/consultation/invoice data is fetched through the SvelteKit server
  routes (`api/patients/...`), which carry `X-API-KEY` + `X-Request-Timestamp` + Bearer.
  No direct browser → `:5100` backend call. No API key visible in client-side network
  requests.

---

## Regression checks (already-built capabilities must still work)

- **R-1:** Adding a patient via `(app)/patients/new` still succeeds and the new patient
  appears in the list.
- **R-2:** Searching by term on `(app)/patients` still filters correctly and paginates.

---

## Exit criteria

All TS-001.1 → TS-001.13 pass, with **TS-001.9 (no cross-cabinet leak)** and
**TS-001.12 (auth redirect)** treated as **blocking** — neither may be waived.
Regression checks R-1, R-2 still green.
