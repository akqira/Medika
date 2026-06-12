Analyze the current git state and help craft a perfect conventional commit message. Follow these steps precisely.

## Step 1 — Gather git state

Run all of these in parallel:
- `git status --short` — list all changed files (staged and unstaged)
- `git diff --staged` — full diff of staged changes
- `git diff` — full diff of unstaged changes
- `git log --oneline -10` — recent commits to understand style
- `git stash list` — note if there are stashes

If `$ARGUMENTS` is provided, treat it as a hint (e.g. "fix", "feat", "chore", or a short description of what changed).

## Step 2 — Analyze changes

### 2a — Detect change type
Choose the most appropriate conventional commit type:

| Type | When to use |
|---|---|
| `feat` | New feature or capability visible to the user |
| `fix` | Bug fix |
| `refactor` | Code restructure without behavior change |
| `chore` | Tooling, config, dependencies, non-user-facing maintenance |
| `docs` | Documentation only |
| `style` | Formatting, whitespace — no logic change |
| `perf` | Performance improvement |
| `test` | Adding or fixing tests |
| `ci` | GitHub Actions / CI pipeline changes |
| `build` | Build system, Docker, project file changes |
| `revert` | Reverting a previous commit |

A `!` suffix (e.g. `feat!:`) signals a **breaking change** (MAJOR version bump).

### 2b — Detect scope from file paths
Map changed files to the most relevant scope:

| Files touched | Scope |
|---|---|
| `apps/backend/.../Auth/` or `routes/login/` or `hooks.server.ts` | `auth` |
| `apps/backend/.../Patients/` or `routes/.../patients/` | `patients` |
| `apps/backend/.../Medical/` or `routes/.../consultation/` | `consultation` |
| `apps/backend/.../Scheduling/` or `routes/.../schedule/` | `scheduling` |
| `apps/backend/.../Finance/` or `routes/.../finance/` | `finance` |
| `apps/frontend/src/lib/` or `app.css` | `frontend` |
| `apps/backend/src/Medika.Domain/` or `Medika.Application/` | `domain` |
| `apps/backend/src/Medika.Infrastructure/Persistence/` | `db` |
| `.claude/` or `.github/` | `dev` |
| `.github/workflows/` | `ci` |
| `apps/backend/src/Medika.Api/Program.cs` or `appsettings*` | `api` |

**If changes touch multiple scopes**: use the primary scope (where most changes are). If truly cross-cutting, omit the scope.

### 2c — Write the subject line
Rules:
- Format: `type(scope): description`
- Max 72 characters total
- Description: imperative mood, lowercase, no trailing period ("add patient search" not "Added patient search.")
- Be specific: "fix(auth): handle missing medika_token cookie on layout redirect" beats "fix(auth): fix auth bug"

### 2d — Detect breaking changes
Flag as breaking (`type!:` or `BREAKING CHANGE:` footer) if:
- An API endpoint signature changed
- A MongoDB collection schema changed in a non-backwards-compatible way
- An existing cookie or session field was renamed/removed
- A public config key was renamed

## Step 3 — Assess version impact

Check whether a version bump is warranted. Read `apps/frontend/package.json` for the current version. Apply semver rules:

| Change | Bump |
|---|---|
| Breaking change | MAJOR (`x+1.0.0`) |
| New feature (`feat`) | MINOR (`x.y+1.0`) |
| Bug fix, refactor, chore | PATCH (`x.y.z+1`) |
| Docs, style, ci only | No bump |

Only suggest a bump if the change is release-ready (not a WIP or internal-only commit).

## Step 4 — Check CHANGELOG

Check if `CHANGELOG.md` exists at the project root.

- If it **does not exist** and the change is a `feat` or `fix`: offer to create it with a `## [Unreleased]` section.
- If it **exists**: offer to add an entry under `## [Unreleased]` in Keep a Changelog format:
  ```
  ### Added     ← for feat
  ### Fixed     ← for fix
  ### Changed   ← for refactor / breaking
  ### Removed   ← for deletions
  ```

## Step 5 — Present your analysis

Output a structured proposal:

---
### Proposed commit

```
type(scope): short description

[optional body: why the change was made, context for future readers]

[optional footer: BREAKING CHANGE: description]
[optional footer: Co-Authored-By: ...]
```

**Type**: `feat` / `fix` / etc.
**Scope**: detected scope or "none"
**Breaking change**: yes / no

### Version impact
Current version: `x.y.z`
Suggested bump: PATCH → `x.y.z+1` (or MINOR/MAJOR/none)

### Changelog
[ ] Add entry to `CHANGELOG.md` under `## [Unreleased]`

---

Then ask:
> "Does this look right? Reply with:
> - **yes** — I'll stage all changes, update CHANGELOG if applicable, and commit
> - **edit** — tell me what to change
> - **message only** — just copy the message above, I'll commit manually"

## Step 6 — Execute (only if confirmed)

If the user confirms with **yes**:
1. If CHANGELOG update was accepted: write the entry to `CHANGELOG.md` first
2. Stage all unstaged changes: `git add -A` *(ask if there are untracked files that should be excluded)*
3. Commit with the proposed message using a heredoc:
   ```bash
   git commit -m "$(cat <<'EOF'
   type(scope): description

   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
   EOF
   )"
   ```
4. Report the commit hash and confirm success

**Never commit without explicit user confirmation.**
