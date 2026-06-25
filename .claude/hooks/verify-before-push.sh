#!/usr/bin/env bash
# Verify-before-push tripwire (issue #24 lesson).
# Fires on `git push`. If the push would ship Playwright e2e specs or the shared
# (app) layout, it injects a reminder to run the e2e suite LOCALLY first — because
# those changes break in ways static checks (pnpm check / dotnet build) can't see.
# Non-blocking: the push still proceeds; this only nudges. Reads the tool-call JSON
# on stdin (Claude Code PreToolUse contract).
set -uo pipefail

input=$(cat)

# Only react to git push commands.
printf '%s' "$input" | grep -qiE 'git[[:space:]]+push' || exit 0

# Files this branch would push: commits since the base + staged + unstaged.
base=$(git merge-base HEAD origin/dev 2>/dev/null || git merge-base HEAD origin/main 2>/dev/null || true)
changed=$(
  {
    [ -n "$base" ] && git diff --name-only "$base" HEAD
    git diff --name-only HEAD
    git diff --name-only --cached
  } 2>/dev/null || true
)

if printf '%s' "$changed" | grep -qE 'apps/frontend/e2e/|routes/\(app\)/\+layout'; then
  echo 'verify-before-push: this push changes e2e specs or the (app) layout — run the e2e suite locally first.' 1>&2
  printf '%s' '{"hookSpecificOutput":{"hookEventName":"PreToolUse","additionalContext":"VERIFY-BEFORE-PUSH: this push changes Playwright e2e specs or the shared (app) layout. Run the e2e suite LOCALLY before pushing (start both apps, then: pnpm --filter frontend exec playwright test) — do NOT treat CI as the verification step. If you already ran it this session, proceed."}}'
fi
exit 0
