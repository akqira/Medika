import { describe, it, expect } from 'vitest';
import { isValidDzPhone } from './phone';

describe('isValidDzPhone', () => {
	it('accepts mobiles of every operator (05/06/07, 10 digits)', () => {
		expect(isValidDzPhone('0555123456')).toBe(true); // Ooredoo
		expect(isValidDzPhone('0661234567')).toBe(true); // Mobilis
		expect(isValidDzPhone('0791234567')).toBe(true); // Djezzy
	});

	it('accepts landlines (0 + indicatif, 9 digits)', () => {
		expect(isValidDzPhone('021234567')).toBe(true); // Alger
		expect(isValidDzPhone('031924567')).toBe(true); // Constantine
		expect(isValidDzPhone('041234567')).toBe(true); // Oran
	});

	it('ignores spaces', () => {
		expect(isValidDzPhone('0555 12 34 56')).toBe(true);
		expect(isValidDzPhone('021 23 45 67')).toBe(true);
	});

	it('rejects the old mobile-only gap and malformed numbers', () => {
		expect(isValidDzPhone('12345')).toBe(false); // too short, no leading 0
		expect(isValidDzPhone('0812345678')).toBe(false); // 08 is neither mobile nor fixe
		expect(isValidDzPhone('05551234')).toBe(false); // mobile too short
		expect(isValidDzPhone('02123456')).toBe(false); // landline too short (8)
		expect(isValidDzPhone('0212345678')).toBe(false); // landline too long (10)
		expect(isValidDzPhone('055512345a')).toBe(false); // contains a letter
		expect(isValidDzPhone('')).toBe(false);
	});
});
