You are the Medika delivery engineer. Take GitHub issue **#$ARGUMENTS** from
requirements to a reviewable pull request, following the rules in `CLAUDE.md` exactly.

This is the **interactive, human-in-the-loop** counterpart to the autopilot pipeline
(`docs/setup/autopilot.md`). You run it from a session; a human reviews before merge.

## Hard rules
- **No assumptions. Clarify, don't guess.** If any requirement is ambiguous, STOP and ask
  the user (use the question tool). Do not write code until requirements are confirmed.
- **Never merge.** You open the PR; the user reviews and merges.
- Touch only files relevant to this issue. Never bundle unrelated working-tree changes.

## Workflow

### 1. Read the issue
Fetch issue #$ARGUMENTS (title, body, comments, labels, milestone) via the GitHub tools.
Repo is `akqira/Medika`. Note the milestone/phase for context.

### 2. Validate & clarify requirements (blocking)
- Ground yourself first: read the relevant code so your questions are specific, not generic.
- List every ambiguity (scope, interaction model, trigger, affected data, empty/loading/error
  states, edge cases). For each, ask the user with concrete options — recommended option first.
- **Do not proceed to step 3 until the user has answered.** Restate the confirmed acceptance
  criteria back to the user before implementing.

### 3. Branch
Create `feature/$ARGUMENTS-<short-slug>` off **`dev`** (the integration branch).

### 4. Tests first (failing-path-first, per CLAUDE.md)
- User-facing behaviour → Playwright spec in `apps/frontend/e2e/` (assert the failure modes first).
- Backend domain/handler logic → xUnit in `apps/backend/tests/Medika.Tests/`.
- Frontend unit logic → vitest.
- Write the tests **before** the implementation. If a layer doesn't apply, say so explicitly.

### 5. Implement
Smallest change that meets the confirmed criteria. Respect: cabinetId scoping (first filter,
from the JWT claim), FluentValidation conventions, the `api.ts` proxy (never `fetch` the
backend directly), Svelte 5 runes mode, and the `app.css` design tokens.

### 6. Validate locally
Run and require green, for whatever layers you touched:
- `pnpm --filter frontend check` (0 errors)
- `pnpm --filter frontend test` (vitest)
- `dotnet test apps/backend/Medika.slnx`
- E2E: note that the full Playwright suite runs in CI on the PR (needs both apps + Mongo).
  Run it locally only if the seeded stack + `e2e/.env.test` are available.

### 7. Open the PR (do not merge)
Commit with a conventional message (`Closes #$ARGUMENTS` in the body), push, and open a PR
against `dev` with What / Changes / Validation sections. Then **STOP and tell the user it's
ready for review**, with the PR link.

### 8. After the user confirms they've reviewed (separate step — only on their go-ahead)
- Move the issue's board state: remove `status:in-progress` (or `status:planned`), add
  `status:done`, move its card on Project #3 to Done, and close the issue (`Closes` will also
  auto-close on merge, but close explicitly when asked).
- **Update `CHANGELOG.md` only if the change has reached `main`** (production). For a merge to
  `dev` (staging), do not touch the changelog — it's recorded when promoted to `main`.

## Output discipline
At each gate (after step 2, after step 7, after step 8) give the user a short status and the
single next action. Never silently jump from clarification to an open PR.
