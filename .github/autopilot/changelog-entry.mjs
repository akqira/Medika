// Prepend THIS PR's entry under [Unreleased] in CHANGELOG.md, idempotently.
//
// Runs PRE-merge on the PR branch (see .github/workflows/changelog.yml) so the
// entry rides through the e2e-gated merge — the old post-merge push straight to
// `dev` is rejected now that `e2e` is a required status check on the branch.
//
// Env: PR_NUMBER, PR_TITLE, PR_BODY, PR_BRANCH.
import { readFileSync, writeFileSync, existsSync } from 'node:fs';

const path = 'CHANGELOG.md';
const num = (process.env.PR_NUMBER || '').trim();
const title = (process.env.PR_TITLE || '').trim();
const body = process.env.PR_BODY || '';
const branch = process.env.PR_BRANCH || '';

if (!num || !title) {
  console.log('No PR number/title in env — nothing to record.');
  process.exit(0);
}

// Append "closes #N" when the PR links an issue (body keyword or feature/<n>-… branch).
function linkedIssue() {
  const m = body.match(/\b(?:close[sd]?|fixe?[sd]?|resolve[sd]?)\s+#(\d+)/i);
  if (m) return Number(m[1]);
  const b = branch.match(/^(?:feature|fix|chore)\/(\d+)-/);
  if (b) return Number(b[1]);
  return null;
}

const issue = linkedIssue();
const entry = `- ${title} (#${num}${issue ? `, closes #${issue}` : ''})`;

let md = existsSync(path) ? readFileSync(path, 'utf8') : '# Changelog\n\n## [Unreleased]\n';
const lines = md.split('\n');

let idx = lines.findIndex((l) => l.trim() === '## [Unreleased]');
if (idx === -1) {
  // No Unreleased section yet — open one right after the top title line.
  lines.splice(1, 0, '', '## [Unreleased]');
  idx = lines.findIndex((l) => l.trim() === '## [Unreleased]');
}

// Section body = bullet lines from just after the heading to the next "## " heading.
let end = idx + 1;
while (end < lines.length && !lines[end].startsWith('## ')) end++;

// Keep existing bullets, but drop any prior auto-entry for THIS PR (handles title
// edits / re-runs). A PR's own marker is "(#<num>," or "(#<num>)".
const prMarker = new RegExp(`\\(#${num}(?:,|\\))`);
const kept = lines
  .slice(idx + 1, end)
  .filter((l) => l.startsWith('- ') && !prMarker.test(l));

const rebuilt = [...lines.slice(0, idx + 1), entry, ...kept, '', ...lines.slice(end)];
md = rebuilt.join('\n').replace(/\n{3,}/g, '\n\n');

writeFileSync(path, md);
console.log(`CHANGELOG: ${entry}`);
