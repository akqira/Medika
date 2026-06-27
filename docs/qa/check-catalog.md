# QA Check Catalog

The single source of truth for the **`/qa-sweep`** agent (driven by the
`qa-explorer` subagent). It lists the repeated checks to run against each page,
as a *real user trying to break the app*. Refine this file over time — adding a
check here makes every future sweep cover it.

> **Scope: negative / validation-only.** The sweep only sends inputs the app is
> **supposed to reject**, and only submits when the expected result is a *rejection*
> (no successful write). It must never create, edit, or delete real data. Reading
> pages, opening modals, and submitting invalid forms are all fine. Submitting a
> *valid* happy-path form is **out of scope** (would mutate the DB).

## How a sweep works

For each target page the agent:
1. Logs in (seeded doctor) and navigates to the page. Reuses the `login` flow
   from `apps/frontend/e2e/helpers.ts` semantics (email/password from env).
2. Runs the **generic page checks** below.
3. For every form/input on the page, runs the **form battery** scoped to the
   field's type.
4. Runs the **per-page checks**.
5. For each failure: capture a **screenshot** + the **console errors** + any
   **network 5xx/4xx** seen, and emit a structured finding.
6. Dedup against open issues, then file the survivors (see Issue flow).

## What counts as a failure (file an issue)

- An invalid input is **accepted** (no error shown, or it submits/persists).
- A validation message is **missing, wrong, or untranslated** (UI is French).
- The page throws a **console error** or an uncaught exception on load/interaction.
- A backend call returns **5xx**, or a **4xx** surfaces as a raw error / white screen
  instead of a friendly message.
- The UI **breaks**: layout overflow, element overlapping/un-clickable, infinite
  spinner, empty state where data is expected, dialog that can't be closed.
- A control is **unreachable by keyboard** or has no accessible label.
- **Data loss**: a value the user typed disappears after an unrelated interaction
  (the class of bug behind issue #85).

## What is NOT a failure (do not file)

- An invalid input that is **correctly rejected** with a clear French message.
- Intentional empty states ("Aucun patient", etc.).
- Cosmetic preferences not covered by a check here.
- Anything requiring a *successful* write to verify (out of scope).

When unsure, **lower the severity and still file with `status:needs-triage`** — the
human validates. Better a triaged false-positive than a missed real bug, but keep
confidence in the finding so triage is fast.

---

## Generic page checks (every page)

| ID | Check | Expected |
|----|-------|----------|
| G1 | Page loads | No console errors, no 5xx, primary heading visible |
| G2 | Auth guard (for `(app)/*`) | Visiting while logged-out → redirect to `/login` |
| G3 | No raw error leak | No stack trace / English framework error / `undefined`/`null` rendered as text |
| G4 | Layout integrity | No horizontal overflow at 1280px and at 375px (mobile); no overlapping clickables |
| G5 | Loading/empty/error states | Spinners resolve; empty states show a friendly message, not a blank panel |
| G6 | Navigation | Nav links work; active page highlighted; logout works (see CLAUDE.md logout gotcha) |

## Form battery (per input, by type)

Apply each row to every field of that type in a form. Submit only to confirm a
*rejection*; never to confirm a success.

### Required fields
- Submit with the field **empty** → must show a required error, must not submit.
- Submit with **whitespace only** (`"   "`) → treated as empty, rejected.

### Text / name fields
- **Length overflow**: paste a very long string (e.g. 500+ chars) → rejected or
  safely capped, never a 5xx or broken layout.
- **Letters-only fields** (e.g. nom/prénom): enter digits/symbols (`J0hn@`) → rejected.
- **XSS probe**: enter `<script>alert(1)</script>` → stored/echoed **escaped**, never executed (no dialog — see browser-automation dialog warning in the harness rules).
- **Leading/trailing spaces**: `"  abc  "` → trimmed or handled, not persisted raw.

### Email fields
- Malformed: `abc`, `a@`, `a@b`, `a b@c.com`, missing TLD → rejected.
- Valid-but-unusual: `a+tag@sub.domain.co` → accepted (do not flag).

### Phone fields (Algerian: mobile 05/06/07 or fixed 02/03/04, 10 digits)
- Letters / symbols → stripped or rejected (issue #84 contract).
- Too short (`061`), too long (11+ digits), wrong prefix (`0123`) → rejected.

### Number / amount fields
- Below min (`0`, negative) → rejected (e.g. charge amount `min=1`).
- Non-numeric text → rejected.
- Huge values / scientific notation (`1e9`) → handled sanely.

### NSS / identifier fields (e.g. 15-digit NSS)
- Wrong length (14, 16 digits), non-digit chars → rejected.

### Select / dropdown
- Default "— Choisir —" left selected on a required select → rejected.

### Multi-step wizards (e.g. patients/new)
- Field `name` attributes must survive step changes (hidden `{#if}` steps drop
  inputs from the DOM — see CLAUDE.md). Fill step 1, advance, go back → values retained.
- Per-step validation blocks "Next" on invalid input.

### Cross-field / re-render integrity (the #85 class)
- Fill several fields, then interact with **another** field (type in it, toggle,
  open a picker). Re-check the first fields **still hold their values**.
- Double-click submit rapidly → no double POST / no duplicate.
- Press **Escape** in a modal/form → closes cleanly; **Enter** submits the intended form.

---

## Per-page checks

> Forms are listed with their fields so the battery can be scoped. Read-only pages
> still get the generic checks.

### `login` (public)
- Fields: email, password. Battery: required, email format.
- Wrong password / unknown email → generic error, **no account enumeration**
  (same message for both).
- Already-authenticated visit → redirect to `/dashboard`.

### `forgot-password` (public)
- Field: email. Required + format. Submitting any email → neutral confirmation
  (no enumeration).

### `reset-password` (public)
- Fields: new password, confirm. Mismatch → rejected; `< 8` chars → rejected;
  missing/expired token → friendly error, not a crash.

### `(app)/profile`
- **Cabinet** tab: cabinetName, specialty, rppsNumber, cabinetAddress, wilaya, phone.
  - Phone battery (#84). Re-render integrity (#85): fill name/order/address, edit
    phone, fields must survive.
- **Compte** tab: firstName, lastName, email. Required + email format; duplicate
  email → friendly 400, not a crash.
- **Sécurité** tab: current/new/confirm password. Mismatch, `<8`, wrong current →
  rejected (non-mutating: never actually changes the password).

### `(app)/patients/new`
- Multi-step wizard. Battery on: nom/prénom (letters-only), phone, email, NSS (15-digit).
  Per-step required validation; step navigation retains values.

### `(app)/patients` (list + search)
- Search: empty query state, min-length behavior, no-result empty state, keyboard
  nav on empty list is a no-op.

### `(app)/patients/[id]` (file view)
- Read-only. Generic checks. Invalid/non-existent id → 404-style friendly page,
  not a crash or another cabinet's data (cabinet scoping).

### `(app)/consultation`
- Save with **no patient selected** → rejected. Add/remove médicament rows behave.
  Finalize asks for confirmation. (Non-mutating: don't actually finalize/save valid.)

### `(app)/finance`
- Charge form: category (required), description (required), amount (`min=1`: reject
  `0`/negative). 

### `(app)/schedule`
- Booking modal: no-patient guard, empty search, min-2-char search, Escape/Cancel
  closes. (Non-mutating: don't actually book.)

### `(app)/team`
- Staff create form: required fields, email format, role required. (Non-mutating:
  don't actually create a user.) Role-permission UI renders per role.

### `(app)/dashboard`, `(app)/actes`
- Generic checks (largely read-only / config surfaces).

---

## Issue flow

For each surviving finding the agent files (or, in report-first mode, lists):

- **Title**: `[QA] <page> — <one-line symptom>`
- **Labels**: `type:bug`, `qa:auto`, `status:needs-triage`
- **Body**: the template below, including the embedded screenshot.

### Dedup signature
To avoid re-filing the same problem each run, compute a stable signature
`qa:<page-route>:<check-id>:<field>` and put it in a trailing HTML comment in the
issue body, e.g. `<!-- qa-sig: qa:/profile:RE85:cabinetName -->`. Before filing,
search open issues (`gh issue list --label qa:auto --search "<sig>"` / `gh search issues`).
If a matching open issue exists, **skip** (optionally add a "still present on
<date>" comment) instead of creating a duplicate.

### Issue body template
```markdown
## Symptôme
<what a user sees go wrong, 1–2 lines>

## Page
`<route>` — <env: local | staging>

## Étapes de reproduction
1. …
2. …
3. …

## Résultat attendu
<what should happen>

## Résultat observé
<what actually happened — include console error / HTTP status if any>

## Preuve
![capture](<raw.githubusercontent.com screenshot URL, or attach>)

## Gravité estimée
<low | medium | high — the human confirms/overrides via a priority:* label>

<!-- qa-sig: <signature> -->
_Auto-détecté par /qa-sweep le <date>. À valider (status:needs-triage)._
```
