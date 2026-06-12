import type { PatientDetail, PatientInvoice } from '$lib/types/api';

/**
 * Orchestrates loading a full patient file (US-001): patient core + consultations
 * + invoices. Pure and dependency-injected (takes an API client) so the not-found /
 * no-leak and section-isolation rules are unit-testable without the SvelteKit runtime.
 */

/** Minimal slice of the server `api` facade this module needs. */
export interface ApiClient {
	get<T>(path: string, token?: string): Promise<T>;
}

/** Row shape that fits BOTH the doctor (full) and receptionist (metadata-only) payloads — ADR-002. */
export interface ConsultationRow {
	consultationId: string;
	date: string;
	isFinalized: boolean;
	reason?: string;
	diagnosis?: string;
	tariff?: number;
	prescriptionCount?: number;
	appointmentId?: string;
}

/** A history section loads independently: a failure here must not blank the whole page (AC-11). */
export interface Section<T> {
	data: T;
	failed: boolean;
}

export interface PatientFile {
	patient: PatientDetail;
	consultations: Section<ConsultationRow[]>;
	invoices: Section<PatientInvoice[]>;
}

/** Thrown when the patient core is a 404 — unknown id OR cross-cabinet, indistinguishable (AC-7). */
export class PatientNotFoundError extends Error {
	constructor() {
		super('Patient introuvable');
		this.name = 'PatientNotFoundError';
	}
}

function hasStatus(e: unknown, status: number): boolean {
	return typeof e === 'object' && e !== null && 'status' in e &&
		(e as { status: unknown }).status === status;
}

async function loadSection<T>(load: () => Promise<T>, fallback: T): Promise<Section<T>> {
	try {
		return { data: await load(), failed: false };
	} catch (e) {
		console.error('[patient-file] section load failed:', e);
		return { data: fallback, failed: true };
	}
}

export async function loadPatientFile(
	client: ApiClient,
	token: string,
	id: string
): Promise<PatientFile> {
	// Patient core first — its outcome decides whether there is a page at all.
	let patient: PatientDetail;
	try {
		patient = await client.get<PatientDetail>(`/api/patients/${id}`, token);
	} catch (e) {
		// 404 (unknown id or another cabinet's patient) → clean not-found, no information leak.
		if (hasStatus(e, 404)) throw new PatientNotFoundError();
		throw e; // genuine failure (500/503/...) → surfaced as a page error by the caller.
	}

	// History sections load in parallel and are isolated from each other (AC-11).
	const [consultations, invoices] = await Promise.all([
		loadSection<ConsultationRow[]>(
			() =>
				client
					.get<{ items?: ConsultationRow[] }>(
						`/api/patients/${id}/consultations?page=1&pageSize=20`,
						token
					)
					.then((r) => r.items ?? []),
			[]
		),
		loadSection<PatientInvoice[]>(
			() => client.get<PatientInvoice[]>(`/api/patients/${id}/invoices`, token),
			[]
		)
	]);

	return { patient, consultations, invoices };
}
