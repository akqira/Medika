Reset the `medika_dev` database to a blank "new joiner doctor" slate.

Optional argument: an email to keep instead of the default. $ARGUMENTS

## Your job
1. Run the reset script (keeps only the doctor user, wipes all business data):

   ```bash
   node .claude/scripts/reset-newjoiner-db.mjs $ARGUMENTS
   ```

   - With no argument it keeps `kader.kebir@gmail.com`.
   - The script reads the Atlas URI from the gitignored
     `appsettings.Development.json`, refuses to run on a non-`dev` database,
     and aborts (deletes nothing) if the keep-user is not found.

2. Report the deletion counts and the remaining user(s) from the script output.
   Do **not** re-query the DB to "verify" — the script already prints the final
   state. Only investigate further if the script exits non-zero.

Note: the kept user retains its existing `cabinetId`; no re-seed happens on the
next backend start (`MongoDbInitializer` backfill is a no-op with one user).
