#!/usr/bin/env node
// Reset medika_dev to a "new joiner doctor" blank slate:
// wipe ALL business data and keep only the configured doctor user.
//
// Usage:  node .claude/scripts/reset-newjoiner-db.mjs [keepEmail]
//   keepEmail defaults to kader.kebir@gmail.com
//
// The MongoDB URI + DB name are read from the gitignored
// appsettings.Development.json (no secret is committed). Uses mongosh on PATH.

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(here, '..', '..');
const settingsPath = resolve(
  repoRoot,
  'apps/backend/src/Medika.Api/appsettings.Development.json',
);

const keepEmail = (process.argv[2] || 'kader.kebir@gmail.com').trim();

let cfg;
try {
  cfg = JSON.parse(readFileSync(settingsPath, 'utf8'));
} catch (e) {
  console.error(`Could not read ${settingsPath}: ${e.message}`);
  process.exit(1);
}

const uri = cfg?.MongoDB?.ConnectionString;
const dbName = cfg?.MongoDB?.DatabaseName;
if (!uri || !dbName) {
  console.error('MongoDB.ConnectionString / DatabaseName missing in appsettings.Development.json');
  process.exit(1);
}

// Safety guard: refuse to run against anything that isn't an obvious dev DB.
if (!/dev/i.test(dbName)) {
  console.error(`Refusing to wipe non-dev database "${dbName}".`);
  process.exit(1);
}

const evalJs = `
const keep = ${JSON.stringify(keepEmail)};
const before = db.users.countDocuments({ email: keep });
if (before === 0) {
  print("ABORT: user " + keep + " not found in " + db.getName() + " — nothing deleted.");
  quit(1);
}
const businessCollections = ["patients","invoices","consultations","appointments","charges","acts","audit_logs"];
let report = { users: db.users.deleteMany({ email: { $ne: keep } }).deletedCount };
for (const c of businessCollections) {
  if (db.getCollectionNames().includes(c)) {
    report[c] = db.getCollection(c).deleteMany({}).deletedCount;
  }
}
print("Deleted: " + JSON.stringify(report));
print("Remaining users: " + db.users.countDocuments());
db.users.find({}, { email: 1, role: 1, cabinetId: 1 }).forEach(u => printjson(u));
`;

console.log(`Resetting "${dbName}" — keeping only ${keepEmail} ...`);
const res = spawnSync('mongosh', [uri, '--quiet', '--eval', evalJs], {
  stdio: 'inherit',
  shell: false,
});

process.exit(res.status ?? 1);
