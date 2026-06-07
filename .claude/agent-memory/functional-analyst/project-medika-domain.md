---
name: project-medika-domain
description: Core domain entities, enumerations, and business concepts defined in Medika.Domain
metadata:
  type: project
---

Medika is a medical cabinet management SaaS for Algerian solo GPs (médecins généralistes).

**Domain aggregates:**
- Patient — identity, contact, NSS, blood group, allergies, medical history, insurance (CNAS / CASNOS / Military / None), wilaya, emergency contact, current treatment
- Consultation — vital signs (BP, pulse, weight, temp, SpO2), clinical exam, diagnosis, notes, prescription lines, tariff, finalization flag; creates Invoice + updates Appointment on Complete()
- Appointment — statuses: Pending, Confirmed, InProgress, Completed, Cancelled, NoShow; types: FirstVisit, FollowUp, LabResults, Prescription, Certificate, Urgent, Other; holds ConsultationId after completion
- Invoice — linked to Consultation; statuses: Pending, Paid, Cancelled; payment methods: Cash, BankTransfer, Check, Other; format "F-2026-001"
- Charge (cabinet operating expense) — categories: Rent, Internet, Phone, Insurance, Equipment, Maintenance, Accounting, Other; optional IsRecurring flag
- User — roles: Doctor, Receptionist, Patient; Doctor has Specialty + OrderNumber fields; Patient role can link to PatientId

**Algeria-specific considerations:**
- All 48 wilayas are explicitly listed in the frontend
- Currency is Algerian Dinar (DA)
- Insurance codes: CNAS, CASNOS, Military, None
- Locale: fr-DZ (French language, Algerian formatting)
- NSS = Numéro de Sécurité Sociale

**Why:** Understanding these domain boundaries prevents creating requirements that conflict with the existing model and ensures Algeria-specific data (wilaya, NSS, insurance codes) is always included.

**How to apply:** Reference these entities directly when writing user stories and acceptance criteria. Always use "DA" for currency. Always include the 4 insurance options when insurance is relevant.
