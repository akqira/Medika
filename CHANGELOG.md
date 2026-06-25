# Changelog

All notable changes to Medika are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/); the autopilot post-merge job prepends
entries under **[Unreleased]** as PRs merge into `dev`.

## [Unreleased]
- feat(email): deliver password-reset links via Brevo (#74, closes #73)
- ci(frontend): run vitest unit tests in CI (#59) (#70, closes #59)
- feat(auth): role & permission system (#24) (#68, closes #24)
- feat(auth): role & permission system — doctor-admin creates secretaries with customisable, per-permission access; endpoints gated by `Permissions(...)` claims; role-aware nav + Équipe management page (#24)
- feat(navbar): wire global search to the patients page (#54) (#65, closes #54)
