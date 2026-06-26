// Post-merge GitHub sync. Resolves the issue a merged PR closed, then:
//   1. label status:in-progress -> status:done
//   2. close the issue
//   3. move its Projects v2 card Status -> Done (needs PROJECTS_TOKEN)
//   4. prepend a CHANGELOG.md entry (committed by the workflow step)
// Each step is best-effort and logs; a board-permission miss never blocks the rest.
import { readFileSync, writeFileSync, existsSync } from 'node:fs';

const token = process.env.GH_TOKEN;
const [owner, repo] = process.env.REPO.split('/');
const projectOwner = process.env.PROJECT_OWNER;
const projectNumber = Number(process.env.PROJECT_NUMBER);
const pr = { number: process.env.PR_NUMBER, title: process.env.PR_TITLE, body: process.env.PR_BODY || '', branch: process.env.PR_BRANCH || '', url: process.env.PR_URL };

const gh = async (path, init = {}) => {
  const res = await fetch(`https://api.github.com${path}`, {
    ...init,
    headers: { authorization: `Bearer ${token}`, accept: 'application/vnd.github+json', 'content-type': 'application/json', ...(init.headers || {}) },
  });
  if (!res.ok) throw new Error(`${init.method || 'GET'} ${path} → ${res.status} ${await res.text()}`);
  return res.status === 204 ? null : res.json();
};

const graphql = async (query, variables) => {
  const res = await fetch('https://api.github.com/graphql', {
    method: 'POST',
    headers: { authorization: `Bearer ${token}`, 'content-type': 'application/json' },
    body: JSON.stringify({ query, variables }),
  });
  const json = await res.json();
  if (json.errors) throw new Error(JSON.stringify(json.errors));
  return json.data;
};

// --- Resolve the closed issue number ---
function resolveIssue() {
  const m = pr.body.match(/\b(?:close[sd]?|fixe?[sd]?|resolve[sd]?)\s+#(\d+)/i);
  if (m) return Number(m[1]);
  const b = pr.branch.match(/^(?:feature|fix|chore)\/(\d+)-/);
  if (b) return Number(b[1]);
  return null;
}

const issue = resolveIssue();
if (!issue) {
  console.log('No linked issue found in PR body/branch — nothing to sync.');
  process.exit(0);
}
console.log(`Linked issue: #${issue}`);

// --- 1 + 2: labels + close ---
try {
  await gh(`/repos/${owner}/${repo}/issues/${issue}/labels/status:in-progress`, { method: 'DELETE' }).catch(() => {});
  await gh(`/repos/${owner}/${repo}/issues/${issue}/labels`, { method: 'POST', body: JSON.stringify({ labels: ['status:done'] }) });
  await gh(`/repos/${owner}/${repo}/issues/${issue}`, { method: 'PATCH', body: JSON.stringify({ state: 'closed', state_reason: 'completed' }) });
  console.log('Issue labelled status:done and closed.');
} catch (e) {
  console.error('[sync] label/close failed:', e.message);
}

// --- 3: move board card to Done ---
try {
  const data = await graphql(
    `query($login:String!, $number:Int!){
       user(login:$login){ projectV2(number:$number){ id
         field(name:"Status"){ ... on ProjectV2SingleSelectField { id options { id name } } }
       }}}`,
    { login: projectOwner, number: projectNumber }
  );
  const project = data.user.projectV2;
  const statusField = project.field;
  const doneOption = statusField.options.find((o) => /done|termin|livr/i.test(o.name));

  // Find the project item for this issue.
  const issueData = await graphql(
    `query($owner:String!, $repo:String!, $number:Int!){
       repository(owner:$owner, name:$repo){ issue(number:$number){ id
         projectItems(first:20){ nodes { id project { id } } } } } }`,
    { owner, repo, number: issue }
  );
  const item = issueData.repository.issue.projectItems.nodes.find((n) => n.project.id === project.id);

  if (item && doneOption) {
    await graphql(
      `mutation($project:ID!, $item:ID!, $field:ID!, $value:String!){
         updateProjectV2ItemFieldValue(input:{ projectId:$project, itemId:$item, fieldId:$field, value:{ singleSelectOptionId:$value } }){ projectV2Item { id } } }`,
      { project: project.id, item: item.id, field: statusField.id, value: doneOption.id }
    );
    console.log(`Board card moved to "${doneOption.name}".`);
  } else {
    console.warn('[sync] board item or Done option not found — skipped board move.');
  }
} catch (e) {
  console.error('[sync] board move failed (PROJECTS_TOKEN missing project scope?):', e.message);
}

// --- 4: prepend CHANGELOG entry ---
try {
  const path = 'CHANGELOG.md';
  const entry = `- ${pr.title} (#${pr.number}, closes #${issue})`;
  let md = existsSync(path) ? readFileSync(path, 'utf8') : '# Changelog\n\nAll notable changes to Medika.\n\n## [Unreleased]\n';
  if (!md.includes('## [Unreleased]')) md = md.replace(/^(# Changelog[\s\S]*?\n)/, `$1\n## [Unreleased]\n`);
  md = md.replace('## [Unreleased]', `## [Unreleased]\n${entry}`);
  writeFileSync(path, md);
  console.log('CHANGELOG.md updated.');
} catch (e) {
  console.error('[sync] changelog update failed:', e.message);
}
