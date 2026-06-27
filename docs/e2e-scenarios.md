# E2E Scenario Library

The **source of truth** for Medika's end-to-end *journeys* — full, happy-path user
stories described in plain language, each mirrored 1:1 by an executable Playwright
spec under `apps/frontend/e2e/journeys/`.

This complements (does not replace) the **failing-situation-first** validation specs
in `apps/frontend/e2e/` (catalogued in the CLAUDE.md E2E table). Those assert *what
must be blocked*; the journeys here assert *what must keep working* — so a new
feature can't silently break the booking → consultation → invoice → document flow.

## How a scenario is born

1. You describe a journey in plain language ("as a doctor, I log in, find a patient,
   create a 2 000 DA consultation, write an ordonnance, print it and email it to …").
2. It gets an ID (`JOURNEY-NN`) and an entry in the table below, with its steps and
   expected outcome.
3. A matching spec `journeys/journey-NN-<slug>.spec.ts` is written and **run locally**.
4. Result is reported back. If it fails because of a **real regression** → a
   `type:bug` issue. If it fails because the **capability isn't built yet** → a
   `type:feature`/story issue. The scenario row links to that issue and is marked
   `blocked` until green.

## Conventions (so the suite stays trustworthy)

- **Self-contained & self-cleaning.** A journey creates its own data with a unique
  tag (e.g. `JOURNEY-01 ${runId}`) and deletes it at the end, so the suite is
  re-runnable in any order and doesn't bloat the dev/e2e DB. (See the finance
  happy-path spec for the create-tag-then-delete pattern.)
- **External effects assert *intent*, not delivery.** Email goes through Brevo, which
  is a **no-op when unconfigured** (dev/CI). A journey asserts the app *triggered* the
  send (network 200 / "Envoyé" UI state / the logged link) — never that a real inbox
  received it. Printing asserts the **PDF was produced/downloaded**, not that paper
  came out. Real delivery is a separate manual smoke check.
- **One ID, one spec.** Keep the table row and the spec's header comment in sync; the
  spec's top comment quotes the plain-language scenario verbatim.

## Status legend

`draft` — written here, no spec yet · `building` — spec being authored ·
`green` — spec passes in CI · `blocked` — fails; see linked issue.

## Scenarios

| ID | Scenario | Spec | Status | Issue |
|----|----------|------|--------|-------|
| JOURNEY-01 | Doctor logs in → finds a patient → creates a 2 000 DA consultation → writes an ordonnance → prints it → emails it to the patient | `journeys/journey-01-consultation-billing.spec.ts` | draft | _tbd_ |

---

### JOURNEY-01 — Consultation, ordonnance, print & email

**As** a doctor, **I want to** see a patient, bill the consultation, prescribe, and
hand the patient their ordonnance (printed + emailed), **so that** a visit is fully
recorded and the patient leaves with their prescription.

**Steps**
1. Log in (seeded doctor).
2. Find an existing patient by name.
3. Open/start a consultation; set the cost to **2 000 DA**.
4. Add at least one médicament to the ordonnance.
5. Finalize the consultation.
6. Print the ordonnance → a PDF is produced.
7. Email the ordonnance to the patient's address → the app reports it sent.

**Expected** — the consultation persists with amount 2 000 DA, the ordonnance lists
the médicament, the PDF download succeeds, and the email send is acknowledged by the
app (intent, per the conventions above).

**Notes** — to be validated against the running app; steps 6–7 depend on whether
print/email-ordonnance are built. First run will classify any gap as bug vs feature.
