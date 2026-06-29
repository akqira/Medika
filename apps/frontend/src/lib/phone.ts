// Centralized Algerian phone validation (issue #124).
//
// One rule, reused by every phone field (patient principal + urgence, quick-add,
// profil cabinet) and mirrored server-side by Medika.Application AlgerianPhone.
//
//   Mobile : 0[5-7] + 10 digits  — 05 Ooredoo, 06 Mobilis, 07 Djezzy.
//   Fixe   : 0[1-4] + 9 digits   — indicatif régional, ex. 021 23 45 67 (Alger).
//
// Spaces are ignored so users can type "021 23 45 67" or "0555 12 34 56".
export const DZ_PHONE_RE = /^0(?:[5-7]\d{8}|[1-4]\d{7})$/;

// Identical wording to the server rule (Medika.Application AlgerianPhone.ErrorMessage).
export const DZ_PHONE_ERROR =
	'Numéro algérien invalide — mobile (0555 12 34 56) ou fixe (021 23 45 67)';

/** True if `value` is a valid Algerian mobile or landline number (spaces ignored). */
export function isValidDzPhone(value: string): boolean {
	return DZ_PHONE_RE.test(value.replace(/\s/g, ''));
}
