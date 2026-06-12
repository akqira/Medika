---
name: project-cabinet-domain-gap
description: Cabinet fields shown in profile UI have no backing domain model — User aggregate only has Specialty and OrderNumber for doctor-specific fields
metadata:
  type: project
---

The profile UI (`apps/frontend/src/routes/(app)/profile/+page.svelte`) collects cabinet fields: `cabinetName`, `cabinetAddress`, `cabinetCity`, `cabinetWilaya`, `cabinetPhone`. None of these exist in the `User` domain aggregate (`apps/backend/src/Medika.Domain/Identity/User.cs`), which only has `Specialty` and `OrderNumber` as doctor-specific fields.

**Why:** The UI was built ahead of the domain model for these cabinet fields, creating a gap that must be resolved before the profile endpoints can be implemented.

**How to apply:** When designing or reviewing the `PATCH /api/profile/cabinet` endpoint or the `GET /api/profile` response, always flag that `User` needs new cabinet fields added (`CabinetName`, `CabinetAddress`, `CabinetCity`, `CabinetWilaya`, `CabinetPhone`). These belong on the `User` aggregate as doctor-profile properties — not a separate `Cabinet` entity, since this is a single-doctor SaaS (Algerian GP cabinet).

See [[project-medika-setup]]
