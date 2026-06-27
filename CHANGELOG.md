# Changelog

All notable changes to Medika are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/); the autopilot post-merge job prepends
entries under **[Unreleased]** as PRs merge into `dev`.

## [Unreleased]
- fix(profile): persist cabinet name/order/address fields (#85) (#95, closes #85)
- fix(profile): enforce Algerian phone format on cabinet field (#84) (#93, closes #84)
- fix(profile): remove duplicate Ville field from cabinet form (#83) (#89, closes #83)
- fix(nav): remove misleading notification bell icon (#82) (#86, closes #82)

## [0.1.0] - 2026-06-26

First production release — `dev` promoted to `main` (closes #60).

- feat(consultation): single-screen 3-column cockpit redesign (#79) (#80, closes #79)
- feat(email): redesign password-reset email to match #76 design (#78, closes #76)
- feat(email): deliver password-reset links via Brevo, with verified domain sender (#74/#77, closes #73)
- ci(frontend): run vitest unit tests in CI (#59) (#70, closes #59)
- feat(auth): role & permission system — doctor-admin creates secretaries with customisable, per-permission access; endpoints gated by `Permissions(...)` claims; role-aware nav + Équipe management page (#24) (#68, closes #24)
- feat(navbar): wire global search to the patients page (#54) (#65, closes #54)
