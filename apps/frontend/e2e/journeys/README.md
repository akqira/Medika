# Journey specs

Executable mirrors of the plain-language journeys in
[`docs/e2e-scenarios.md`](../../../../docs/e2e-scenarios.md). One spec per
`JOURNEY-NN`, named `journey-NN-<slug>.spec.ts`.

These are **happy-path, full-flow** tests (login → … → outcome). They are
deliberately **mutating** — unlike the validation specs in the parent folder — so
each one MUST:

- tag the data it creates with a unique run id and **delete it at the end**, so the
  suite stays re-runnable in any order;
- assert external effects (email/print) as **intent** (network 200 / UI state / PDF
  produced), never real delivery — Brevo is a no-op in dev/CI.

The spec's top comment quotes the scenario verbatim from the registry. Run them with
both apps up, same as the rest of the suite:

```powershell
pnpm --filter frontend exec playwright test e2e/journeys
```
