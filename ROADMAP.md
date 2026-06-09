# Medika — Product Roadmap

**Vision:** The simplest financial management tool for Algerian doctors — know your money, run your cabinet.

---

## Current state
Auth, doctor creation, basic consultation scaffolding. Foundation is there, but nothing is shippable to a real user yet.

---

## MVP — Sprint 1 · Ship to doctor friends
> Goal: a doctor can run a full day end-to-end and see their money.

| # | Feature | Description |
|---|---------|-------------|
| 1 | **Patient file** | Add / search / view patient (name, age, contact, history) |
| 2 | **Consultation** | Create a consultation linked to a patient, record notes/diagnosis |
| 3 | **Ordonnance** | Add medications + instructions → generate a printable PDF (doctor name, patient, date, signature area) |
| 4 | **Payment recording** | Attach a payment to a consultation — amount + method (cash / card / cheque) |
| 5 | **Finance dashboard** | Today's total · This month's total · Recent payments list |

**Success signal:** a doctor friend can open Medika on a Monday morning and close it on Friday having recorded every patient, every payment, and printed every prescription — without asking for help.

---

## Sprint 2 · Complete the practice
> After first doctors are live and giving feedback.

- **Appointment calendar** — daily/weekly schedule management
- **Expense tracking** — log expenses one by one, by category (rent, bills, materials, credit)
- **Income vs. outcome** — simple monthly P&L (earned vs. spent, net balance)
- **Patient self-booking** *(design only, ship when ready)* — public URL for patients to request appointments

---

## Sprint 3 · Group practice
> When expanding beyond solo doctors.

- **Cabinet model** — one cabinet, N doctors
- **Per-doctor + consolidated finance view**
- **Role system** — doctor, secretary/admin

---

## Architecture decisions (build now, pay off later)

These cost almost nothing today but avoid painful migrations later:

1. **Every document carries `cabinetId` + `doctorId`** — even with one doctor, the field is there. Aggregating across doctors later = one extra `$group` stage, not a schema rewrite.
2. **Ordonnance PDF is server-generated** — letterhead is configurable per cabinet from day one.
3. **Finance model built for aggregation** — payments and expenses are their own collections, not embedded, so monthly rollups stay fast as data grows.

---

## Out of scope (not building yet)

- Bank statement import
- Insurance / CNAS integration
- Mobile app
- AI insights
