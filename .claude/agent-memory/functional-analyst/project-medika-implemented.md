---
name: project-medika-implemented
description: Features fully or partially implemented end-to-end in Medika as of June 2026
metadata:
  type: project
---

**Status as of 2026-06-07**

## Fully wired end-to-end

| Feature | Backend endpoint | Frontend route | Notes |
|---|---|---|---|
| Login | POST /api/auth/login | /login | JWT stored in cookie; throttled 5/60s |
| Logout | (cookie clear) | /logout | Server-side only |
| Register user | POST /api/auth/register | None (API-only) | Doctor-role only |
| Create patient | POST /api/patients | /patients/new | 4-step form, full Algeria-specific fields |
| List/search patients | GET /api/patients | /patients | Paginated, debounced search, left-sidebar layout |
| Day schedule (read) | GET /api/schedule/:date | /schedule | Weekly strip nav, timeline view 08:00-18:00 |
| Book appointment | POST /api/appointments | None (no dedicated form) | Backend exists; "Nouveau rendez-vous" button links to /consultation |
| Financial summary | GET /api/finance/summary | /finance | Monthly view, 6-month bar chart trend |
| Mark invoice paid | PATCH /api/invoices/:id/pay | None (no UI trigger yet) | Backend exists |
| Add charge | (handler only, no endpoint file visible) | None | AddChargeCommand + AddChargeHandler exist |
| Dashboard | GET /api/schedule + GET /api/patients | /dashboard | Reads today's appointments + recent patients |

## Partially implemented (UI exists, not wired to API)

| Feature | Gap |
|---|---|
| Consultation form (/consultation) | Save button shows alert("Enregistrement à implémenter côté serveur") — not connected to POST /api/consultations |
| Print prescription | onClick shows alert("Impression à implémenter") |
| Patient detail tabs (Consultations, Ordonnances, Facturation) | Placeholder "Cette section sera disponible prochainement" |
| Start consultation from schedule | "Démarrer la consultation" links to /consultation but passes no patientId/appointmentId |
| Book appointment from schedule | "Nouveau rendez-vous" links to /consultation, not a booking form |

## Profile page — frontend form exists, backend endpoints missing

The profile page calls:
- GET /api/profile
- PATCH /api/profile/cabinet
- PATCH /api/profile/account
- POST /api/profile/change-password

None of these endpoints exist in the Api/Endpoints directory.

**Why:** This map is the authoritative gap list driving next priorities.

**How to apply:** Before writing new requirements, check this list to avoid re-specifying already-implemented features. Update this file after each sprint delivers a feature.
