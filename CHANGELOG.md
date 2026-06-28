# Changelog

All notable changes to Medika are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/); a pre-merge workflow
(`.github/workflows/changelog.yml`) prepends each PR's entry under **[Unreleased]** on
its own branch, so it merges through the e2e gate into `dev`.

## [Unreleased]
- fix(dashboard): replace dev placeholder in empty schedule state (#103) (#143, closes #103)
- feat(consultation): remove prescription quantity (boîtes) field (#125) (#142, closes #125)
- fix(patients): centralize Algerian phone validation, accept landlines (#124) (#141, closes #124)
- fix(team): reject malformed emails (no TLD) on add-member (#100) (#140, closes #100)
- feat(consultation): new MediKa brand design + dedicated ordonnance window (#135) (#137, closes #135)
- feat(patients): make the patient dossier editable (#126) (#131, closes #126)
- feat(notifications): port eGestion Toaster + wire success/error toasts (#129) (#130, closes #129)
- docs(changelog): backfill #121 entry (#123)
- test(e2e): reuse authenticated session across specs (fix cold-start login burn) (#121)
- ci(changelog): generate the changelog entry pre-merge (fix post-merge sync failure) (#122)
- test(e2e): JOURNEY-02 new patient → consultation → ordonnance → honoraire (+ fix #118) (#119, closes #118)
- feat(skill): add /journey for scenario-driven E2E journeys (#117)
- docs(e2e): scenario library (registry + journeys/ mirror) (#116)
- ci(e2e): always report the e2e check on PRs so it can be required (#115)
- fix(patients): surface the >100-char name error instead of silently capping (#114, closes #110)
- fix(finance): reject whitespace-only charge description with 400 (#101) (#112, closes #101)
- fix(forms): French native validation messages via reusable action (#104) (#111, closes #104)
- fix(patients): enforce 100-char max on Nom/Prénom fields (#105) (#110, closes #105)
- fix(patients): show real cabinet total in header, filter count below search (#106) (#108, closes #106)
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
