# Autopilot — autonomous issue → PR pipeline

When an issue enters **In Progress**, a GitHub Action runs Claude Code headless to
clarify, write tests, implement, validate, open a **PR** (never merge), and email Kader
a summary + screenshot. On merge, a second workflow syncs GitHub state.

## The trigger

The signal is the **`status:in-progress` label**, not the board column. GitHub Projects v2
board moves are user-level events that don't trigger repo Actions, so the reliable signal
is the label. Workflow: drop the card into **In Progress**, then add the
`status:in-progress` label (or wire a Projects built-in workflow / `gh` alias to do it).

## Flow

```
issue labelled status:in-progress
        │
        ▼  .github/workflows/autopilot-build.yml
   Claude Code headless (build-prompt.md)
   ├─ thin criteria?  → comment questions, stop  (status: needs_clarification)
   ├─ 3 attempts red? → push branch, comment, stop (status: needs_human)
   └─ green           → open PR vs dev            (status: pr_opened)
        │
        ▼  screenshot.mjs (boots stack, logs in, captures feature route)
        ▼  post-screenshot.mjs (commits shot under docs/ui-validation/, posts it
                                into the PR via a commit-pinned raw URL — UI routes only)
        ▼  notify.mjs (Brevo email + issue comment)
        │
   Kader reviews & MERGES the PR  ← manual gate, always
        │
        ▼  .github/workflows/autopilot-postmerge.yml → postmerge-sync.mjs
   label → status:done · close issue · move board card → Done · prepend CHANGELOG
```

## Required repo secrets (Settings → Secrets and variables → Actions)

| Secret | Used for | Notes |
|--------|----------|-------|
| `CLAUDE_CODE_OAUTH_TOKEN` | running Claude Code headless | subscription auth (`claude setup-token`); no metered API billing. Same secret the QA-sweep workflow uses |
| `PROJECTS_TOKEN` | board moves, PR/issue writes | classic PAT with **`project`** + **`repo`** scope. `GITHUB_TOKEN` cannot touch user Projects v2 |
| `BREVO_API_KEY` | sending the notification email | same provider as the app's transactional email |
| `BREVO_FROM_EMAIL` | email sender | must be a **verified** Brevo sender |

The build workflow **fails fast** if `CLAUDE_CODE_OAUTH_TOKEN` is missing. Email/board steps
degrade gracefully (log a warning, comment on the issue) if their secrets are absent.

## Guardrails

- **Never merges** — Kader reviews and merges every PR.
- **Per-issue concurrency lock** — a re-label can't stack parallel builds.
- **45-min job timeout** + **3 implement→validate attempts**, then a *needs-human* stop
  (branch pushed, issue commented, email sent) instead of force-pushing broken code.
- Touches only its own branch; never pushes to `dev`/`main` except the changelog commit.

## Pausing / disabling

- Pause one issue: remove the `status:in-progress` label before the job starts.
- Pause everything: disable **Autopilot — Build** in the repo Actions tab.
- Hard stop: delete `CLAUDE_CODE_OAUTH_TOKEN` (build fails fast at the guard step).

## Tuning

- Prompt/behaviour: `.github/autopilot/build-prompt.md`
- Screenshot login/route: `.github/autopilot/screenshot.mjs`
- Email/comment copy: `.github/autopilot/notify.mjs`
- Post-merge state sync: `.github/autopilot/postmerge-sync.mjs` (`PROJECT_NUMBER` = 3)
