import { describe, it, expect } from 'vitest';
import { expandPosology } from './posology';

describe('expandPosology', () => {
	it('expands a 4-segment pattern (matin-midi-soir-coucher)', () => {
		expect(expandPosology('1-0-1-0')).toBe('1 matin, 1 soir');
		expect(expandPosology('2-0-0-1')).toBe('2 matin, 1 coucher');
		expect(expandPosology('1-1-1-1')).toBe('1 matin, 1 midi, 1 soir, 1 coucher');
	});

	it('expands a 3-segment pattern (matin-midi-soir)', () => {
		expect(expandPosology('1-0-1')).toBe('1 matin, 1 soir');
		expect(expandPosology('1-1-1')).toBe('1 matin, 1 midi, 1 soir');
	});

	it('tolerates spaces and multi-digit counts', () => {
		expect(expandPosology('  1 - 0 - 2 - 0 ')).toBe('1 matin, 2 soir');
		expect(expandPosology('10-0-0')).toBe('10 matin');
	});

	it('returns null for free text (left untouched)', () => {
		expect(expandPosology('1 cp matin et soir')).toBeNull();
		expect(expandPosology('au besoin')).toBeNull();
		expect(expandPosology('')).toBeNull();
		expect(expandPosology('1-0')).toBeNull(); // only 2 segments
		expect(expandPosology('1-0-1-0-0')).toBeNull(); // 5 segments
	});

	it('returns null when every moment is zero', () => {
		expect(expandPosology('0-0-0')).toBeNull();
		expect(expandPosology('0-0-0-0')).toBeNull();
	});
});
