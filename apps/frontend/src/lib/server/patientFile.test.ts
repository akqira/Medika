import { describe, it, expect, vi } from 'vitest';
import { loadPatientFile, PatientNotFoundError, type ApiClient } from './patientFile';
import type { PatientDetail } from '$lib/types/api';

const PATIENT: PatientDetail = {
	id: 'p1',
	firstName: 'Amine',
	lastName: 'Bensalah',
	age: 34,
	gender: 'M',
	phone: '0550112233',
	allergies: [],
	medicalHistory: [],
	createdAt: '2026-01-01T00:00:00Z'
};

/** Builds an ApiClient that routes by path to the supplied handlers. */
function client(handlers: Record<string, () => Promise<unknown>>): ApiClient {
	return {
		get: vi.fn((path: string) => {
			const key = Object.keys(handlers).find((k) => path.startsWith(k));
			if (!key) throw new Error(`unexpected path ${path}`);
			return handlers[key]() as Promise<never>;
		})
	};
}

/** Mimics the SvelteKit HttpError thrown by RemoteApi on a non-OK status. */
function httpError(status: number) {
	return { status, body: { message: `error ${status}` } };
}

describe('loadPatientFile', () => {
	it('returns patient + both sections on the happy path', async () => {
		const c = client({
			'/api/patients/p1/consultations': () =>
				Promise.resolve({ items: [{ consultationId: 'c1', date: '2026-05-01T00:00:00Z', isFinalized: true, diagnosis: 'Grippe' }] }),
			'/api/patients/p1/invoices': () =>
				Promise.resolve([{ id: 'i1', number: 'F-2026-001', consultationId: 'c1', amount: 2000, status: 'Paid', issuedAt: '2026-05-01T00:00:00Z' }]),
			'/api/patients/p1': () => Promise.resolve(PATIENT)
		});

		const result = await loadPatientFile(c, 'tok', 'p1');

		expect(result.patient.firstName).toBe('Amine');
		expect(result.consultations.failed).toBe(false);
		expect(result.consultations.data).toHaveLength(1);
		expect(result.invoices.failed).toBe(false);
		expect(result.invoices.data).toHaveLength(1);
	});

	it('throws PatientNotFoundError on a 404 patient core (AC-7 no-leak)', async () => {
		const c = client({ '/api/patients/p1': () => Promise.reject(httpError(404)) });
		await expect(loadPatientFile(c, 'tok', 'p1')).rejects.toBeInstanceOf(PatientNotFoundError);
	});

	it('treats a cross-cabinet 404 identically to an unknown id (no information leak)', async () => {
		// Backend returns 404 for another cabinet's patient — same path as unknown id.
		const unknown = client({ '/api/patients/unknown': () => Promise.reject(httpError(404)) });
		const crossCabinet = client({ '/api/patients/other': () => Promise.reject(httpError(404)) });

		const a = await loadPatientFile(unknown, 'tok', 'unknown').catch((e) => e);
		const b = await loadPatientFile(crossCabinet, 'tok', 'other').catch((e) => e);

		expect(a).toBeInstanceOf(PatientNotFoundError);
		expect(b).toBeInstanceOf(PatientNotFoundError);
		expect(a.message).toBe(b.message);
	});

	it('rethrows non-404 patient-core errors for the page to surface (AC-8)', async () => {
		const c = client({ '/api/patients/p1': () => Promise.reject(httpError(503)) });
		await expect(loadPatientFile(c, 'tok', 'p1')).rejects.toMatchObject({ status: 503 });
	});

	it('isolates a failing consultations section without blanking the page (AC-11)', async () => {
		const c = client({
			'/api/patients/p1/consultations': () => Promise.reject(httpError(500)),
			'/api/patients/p1/invoices': () => Promise.resolve([]),
			'/api/patients/p1': () => Promise.resolve(PATIENT)
		});

		const result = await loadPatientFile(c, 'tok', 'p1');

		expect(result.patient.id).toBe('p1');           // page still renders
		expect(result.consultations.failed).toBe(true); // section flagged
		expect(result.consultations.data).toEqual([]);
		expect(result.invoices.failed).toBe(false);     // other section unaffected
	});

	it('isolates a failing invoices section independently', async () => {
		const c = client({
			'/api/patients/p1/consultations': () => Promise.resolve({ items: [] }),
			'/api/patients/p1/invoices': () => Promise.reject(httpError(500)),
			'/api/patients/p1': () => Promise.resolve(PATIENT)
		});

		const result = await loadPatientFile(c, 'tok', 'p1');

		expect(result.invoices.failed).toBe(true);
		expect(result.consultations.failed).toBe(false);
	});

	it('tolerates a consultations payload with no items array (empty state)', async () => {
		const c = client({
			'/api/patients/p1/consultations': () => Promise.resolve({}),
			'/api/patients/p1/invoices': () => Promise.resolve([]),
			'/api/patients/p1': () => Promise.resolve(PATIENT)
		});

		const result = await loadPatientFile(c, 'tok', 'p1');

		expect(result.consultations.failed).toBe(false);
		expect(result.consultations.data).toEqual([]);
	});
});
