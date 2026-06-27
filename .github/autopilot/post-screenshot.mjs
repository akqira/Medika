// Posts the captured feature screenshot INTO the PR conversation (not just the
// email), satisfying the repo rule that every UI/UX PR carries a validation
// screenshot. Commits the PNG under docs/ui-validation/ on the feature branch,
// then embeds it in a PR comment via a commit-SHA-pinned raw.githubusercontent
// URL (survives branch deletion; the repo is public).
//
// Best-effort: any failure here logs a warning and exits 0 — it must never fail
// the build. Runs only when the agent opened a PR for a UI route and the capture
// step produced feature-screenshot.png.
import { existsSync, mkdirSync, copyFileSync, readFileSync } from 'node:fs';
import { execSync } from 'node:child_process';
import path from 'node:path';

const sh = (cmd) => execSync(cmd, { encoding: 'utf8' }).trim();

try {
  const result = JSON.parse(readFileSync('autopilot-result.json', 'utf8'));
  const repo = process.env.REPO;
  const shot = 'feature-screenshot.png';

  if (result.status !== 'pr_opened') {
    console.log('[post-screenshot] status is not pr_opened — skipping.');
    process.exit(0);
  }
  if (!result.screenshotRoute) {
    console.log('[post-screenshot] no screenshotRoute (backend-only change) — skipping.');
    process.exit(0);
  }
  if (!existsSync(shot)) {
    console.warn('[post-screenshot] feature-screenshot.png not found — capture must have failed. Skipping.');
    process.exit(0);
  }

  const issue = result.issue ?? process.env.ISSUE_NUMBER;
  const prRef = result.prNumber ?? result.prUrl;
  const routeSlug = String(result.screenshotRoute).replace(/[^a-z0-9]+/gi, '-').replace(/^-|-$/g, '') || 'feature';

  // Commit the PNG under docs/ui-validation/ on the (already checked-out) feature branch.
  const destDir = path.join('docs', 'ui-validation', 'autopilot', `${issue}-${routeSlug}`);
  const destFile = path.join(destDir, 'feature-validation.png');
  mkdirSync(destDir, { recursive: true });
  copyFileSync(shot, destFile);

  // git user/auth are already configured by the workflow (bot + PROJECTS_TOKEN).
  sh(`git add ${JSON.stringify(destFile)}`);
  // Nothing to commit (e.g. identical re-run) → bail gracefully.
  const staged = sh('git diff --cached --name-only');
  if (!staged) {
    console.log('[post-screenshot] screenshot already committed — skipping.');
    process.exit(0);
  }
  sh(`git commit -q -m ${JSON.stringify(`docs(ui-validation): screenshot for #${issue} (${result.screenshotRoute})`)}`);
  sh(`git push -q origin HEAD:${result.branch}`);

  const sha = sh('git rev-parse HEAD');
  const rawUrl = `https://raw.githubusercontent.com/${repo}/${sha}/${destFile.split(path.sep).join('/')}`;

  const body = [
    '## 🤖 Validation screenshot',
    '',
    `Captured by autopilot against the booted CI stack — route \`${result.screenshotRoute}\` after a seeded-doctor login.`,
    '',
    `![feature validation](${rawUrl})`,
    '',
    `_Pinned to commit \`${sha.slice(0, 7)}\` so the link survives branch deletion._`,
  ].join('\n');

  // gh accepts a PR number or URL as the ref.
  execSync(`gh pr comment ${prRef} --repo ${repo} --body ${JSON.stringify(body)}`, { stdio: 'inherit' });
  console.log('[post-screenshot] posted screenshot to PR', prRef);
} catch (e) {
  console.warn('[post-screenshot] non-fatal failure:', e.message);
  process.exit(0);
}
