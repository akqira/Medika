# ADR-002 — Receptionist visibility into consultation history (metadata only)

**Status:** Accepted
**Date:** 2026-06-12
**Deciders:** Project owner (decision), Solution architect (recording)
**Context trigger:** Architecture review of US-001 (Patient file — view page), Finding 2

---

## Context

US-001 lets a patient file be opened by both **Doctor** and **Receptionist** roles. The
file shows three things sourced from existing endpoints:

| Section | Endpoint | Roles today |
|---------|----------|-------------|
| Identity / contact | `GET /api/patients/{id}` | Doctor, Receptionist |
| Invoices / payments | `GET /api/patients/{id}/invoices` | Doctor, Receptionist |
| Consultation history | `GET /api/patients/{id}/consultations` | **Doctor only** |

The consultations endpoint returns `ConsultationSummary`, which includes
`Reason`, `Diagnosis`, `PrescriptionCount`, and `Tariff` — i.e. **clinical PII**. As wired
today a receptionist opening a patient file gets a 403 on the consultations section, which
contradicts US-001 AC-9 (both roles get the full view).

The underlying question is a confidentiality policy decision, not a bug: **how much of a
patient's clinical history should non-clinical front-desk staff see?**

## Decision

Receptionists may see **consultation metadata only** — that a visit occurred and when —
but **never** the clinical content (reason, diagnosis, clinical exam, notes, prescription
contents/count, vital signs).

Concretely:

1. **Widen the endpoint role, branch the DTO by role.**
   `GetPatientConsultationsEndpoint` adds `"Receptionist"` to `Roles("Doctor",
   "Receptionist")`. The **handler** (`GetPatientConsultationsHandler`) inspects
   `ICurrentUserService.Role` and shapes the response accordingly — authorization to call
   the endpoint and authorization to see fields are decided in **one** place (the handler),
   not split between the route attribute and the UI.

2. **Receptionist-safe projection.** Introduce a trimmed summary for receptionists:
   ```
   ConsultationMetadata(string ConsultationId, DateTime Date, bool IsFinalized)
   ```
   Exclude `Reason`, `Diagnosis`, `Tariff`, `PrescriptionCount`, and everything in
   `ConsultationDetail`. `Tariff` is intentionally excluded here — financial figures reach
   the receptionist through the **invoices** section, not the clinical one.

3. **Doctors are unchanged** — they continue to receive the full `ConsultationSummary`.

4. **Fail safe on unknown roles.** Any role that is neither Doctor nor Receptionist falls
   through to the most restrictive projection (metadata) or is denied — never the full
   clinical summary by default.

## Consequences

- **Positive:** Minimal-disclosure for clinical data; front-desk staff can see a patient's
  visit cadence (useful at the desk) without reading diagnoses. US-001 AC-4/AC-9 become
  consistent with the backend.
- **Negative:** The consultations endpoint is no longer a single fixed DTO — it is
  role-shaped. Any consumer must treat clinical fields as **possibly absent** depending on
  caller role. This must be documented on the endpoint and the SvelteKit proxy.
- **Backend work required (small):** add `ConsultationMetadata`, branch in the handler on
  `currentUser.Role`, widen `Roles(...)`. No new collection, package, or index. Cabinet
  scoping is unchanged (`GetByPatientPagedAsync(cabinetId, …)` already first-parameter
  scoped).

## Impact on US-001 (amended)

- **AC-4** — split by role: doctors see date + diagnosis/summary; receptionists see date +
  "Consultation effectuée" (no diagnosis). Empty state unchanged for both.
- **AC-9** — both roles open the page; the **consultations section content differs by
  role** (metadata-only for receptionist) rather than being hidden or erroring.
- Data-displayed section updated to mark consultation clinical fields as **doctor-only**.

## Alternatives considered

- **Hide the consultations section entirely for receptionists** — rejected by the project
  owner; front-desk staff benefit from knowing a patient's visit history exists/cadence.
- **Give receptionists the full summary** — rejected: exposes diagnoses to non-clinical
  staff with no need-to-know.
- **Two separate endpoints (clinical vs. metadata)** — rejected: duplicates the cabinet/
  patient scoping logic; one role-aware handler is simpler and keeps the authz decision in
  one place.
