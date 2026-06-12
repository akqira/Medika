---
name: project-medika-personas
description: Defined personas for the Medika platform — Doctor, Receptionist, Patient roles
metadata:
  type: project
---

**Primary persona: Dr. (the solo GP)**
- Role in system: Doctor
- Context: Algerian general practitioner running a solo cabinet
- Needs: fast patient lookup, consultation recording with prescription, day schedule at a glance, revenue visibility
- Pain points addressed: paper-based records, manual billing, no appointment history
- Language: French (fr-DZ)

**Secondary persona: Réceptionniste**
- Role in system: Receptionist
- Context: Administrative staff at the cabinet
- Can: create patients, book appointments, mark invoices paid
- Cannot: access consultation content, financial summaries (Doctor-only)

**Tertiary persona: Patient (future)**
- Role in system: Patient
- User entity has LinkedPatientId field — patient portal is architecturally planned but not yet implemented

**Who registers users:** Only a Doctor can register new users (POST /api/auth/register requires Doctor role). Self-registration is not available.

**Why:** The target user is a solo Algerian GP — requirements must reflect their daily workflow, not a multi-doctor clinic model.

**How to apply:** Frame all user stories from the GP's perspective unless explicitly for a Receptionist action. Do not design for multi-doctor scenarios in the current phase.
