// Emails Kader via Brevo and posts an issue comment, tailored to the agent's
// outcome (pr_opened | needs_clarification | needs_human | error). Reads
// autopilot-result.json; attaches feature-screenshot.png when present.
import { readFileSync, existsSync } from 'node:fs';
import { execSync } from 'node:child_process';

const result = JSON.parse(readFileSync('autopilot-result.json', 'utf8'));
const issue = process.env.ISSUE_NUMBER;
const repo = process.env.REPO;
const to = process.env.NOTIFY_TO;
const apiKey = process.env.BREVO_API_KEY;
const from = process.env.BREVO_FROM_EMAIL;

const issueUrl = `https://github.com/${repo}/issues/${issue}`;

const COPY = {
  pr_opened: {
    subject: `[Medika autopilot] PR ready — #${issue}: ${result.summary ? result.summary.slice(0, 60) : ''}`,
    lead: `✅ A pull request is ready for your review.\n\n${result.prUrl}`,
    comment: `🤖 Autopilot opened a PR: ${result.prUrl}\n\n${result.summary ?? ''}`,
  },
  needs_clarification: {
    subject: `[Medika autopilot] Needs clarification — #${issue}`,
    lead: `❓ I couldn't start safely — the acceptance criteria were too thin. I left questions on the issue.`,
    comment: null, // the agent already commented with specifics
  },
  needs_human: {
    subject: `[Medika autopilot] Stuck, needs you — #${issue}`,
    lead: `⚠️ I hit the attempt cap without green tests. The branch \`${result.branch ?? '?'}\` is pushed for inspection.`,
    comment: `🤖 Autopilot stopped after the attempt cap. Branch \`${result.branch ?? '?'}\` pushed for inspection.\n\n${result.summary ?? ''}`,
  },
  error: {
    subject: `[Medika autopilot] Run failed — #${issue}`,
    lead: `❌ The autopilot run failed before producing a result.\n\n${result.summary ?? ''}`,
    comment: `🤖 Autopilot run failed: ${result.summary ?? 'unknown error'}`,
  },
};

const copy = COPY[result.status] ?? COPY.error;

// --- Issue comment (best-effort) ---
if (copy.comment) {
  try {
    execSync(`gh issue comment ${issue} --repo ${repo} --body ${JSON.stringify(copy.comment)}`, { stdio: 'inherit' });
  } catch (e) {
    console.error('[notify] issue comment failed:', e.message);
  }
}

// --- Email via Brevo ---
if (!apiKey || !from) {
  console.warn('[notify] BREVO_API_KEY / BREVO_FROM_EMAIL missing — skipping email (commented on issue instead).');
  process.exit(0);
}

const body =
  `${copy.lead}\n\n` +
  `Issue: ${issueUrl}\n` +
  (result.prUrl ? `PR: ${result.prUrl}\n` : '') +
  `\n— Medika autopilot`;

const payload = {
  sender: { email: from, name: 'Medika Autopilot' },
  to: [{ email: to }],
  subject: copy.subject,
  textContent: body,
};

if (result.status === 'pr_opened' && existsSync('feature-screenshot.png')) {
  payload.attachment = [
    { name: 'feature-screenshot.png', content: readFileSync('feature-screenshot.png').toString('base64') },
  ];
}

const res = await fetch('https://api.brevo.com/v3/smtp/email', {
  method: 'POST',
  headers: { 'api-key': apiKey, 'content-type': 'application/json', accept: 'application/json' },
  body: JSON.stringify(payload),
});

if (!res.ok) {
  console.error('[notify] Brevo send failed:', res.status, await res.text());
  process.exit(0); // don't fail the pipeline on a notification hiccup
}
console.log('[notify] email sent to', to);
