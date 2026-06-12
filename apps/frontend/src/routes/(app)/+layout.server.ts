import { redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken, getUser } from '$lib/server/session';
import type { PagedResult, PatientSummary } from '$lib/types/api';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ cookies }) => {
	const token = getToken(cookies);
	if (!tok