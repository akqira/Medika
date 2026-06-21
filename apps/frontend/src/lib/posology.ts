// Posology shortcut: expand a compact dosing pattern into readable French.
//
// Doctors can type the familiar "1-0-1-0" shorthand (matin-midi-soir-coucher,
// or "1-0-1" for matin-midi-soir) and have it expanded to e.g. "1 matin, 1 soir".
// Anything that isn't a pure numeric pattern is free text and returns null, so
// the Posologie field stays free-text-first — the shortcut is purely additive.

const MOMENTS_3 = ['matin', 'midi', 'soir'] as const;
const MOMENTS_4 = ['matin', 'midi', 'soir', 'coucher'] as const;

// 3 or 4 integer counts separated by hyphens (spaces around hyphens tolerated).
const PATTERN = /^\d+(?:\s*-\s*\d+){2,3}$/;

/**
 * Returns the expanded posology (e.g. "1 matin, 1 soir") for a "1-0-1[-0]"
 * pattern, or null when `raw` isn't such a pattern (free text) or every
 * moment is zero.
 */
export function expandPosology(raw: string): string | null {
	const value = raw.trim();
	if (!PATTERN.test(value)) return null;

	const counts = value.split('-').map((s) => parseInt(s, 10));
	const labels = counts.length === 4 ? MOMENTS_4 : MOMENTS_3;

	const parts = counts
		.map((n, i) => (n > 0 ? `${n} ${labels[i]}` : null))
		.filter((p): p is string => p !== null);

	return parts.length > 0 ? parts.join(', ') : null;
}
