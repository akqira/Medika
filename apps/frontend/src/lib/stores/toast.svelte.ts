// Global toast store — ported from eGestion (apps/front/src/lib/stores/toast.svelte.ts),
// adapted to Medika conventions. One module-level $state array drives the <Toaster />
// mounted in the (app) layout, so any client code can fire a notification with
// `toast.success(...)` / `toast.error(...)` / `toast.info(...)` without prop drilling.
//
// Issue #129 — porter le Toaster d'eGestion et l'appliquer partout.

export type ToastType = 'success' | 'error' | 'info';

export type Toast = {
	id: number;
	message: string;
	type: ToastType;
};

export const toasts = $state<Toast[]>([]);

let nextId = 0;

export function addToast(message: string, type: ToastType = 'success', duration = 4000): number {
	const id = ++nextId;
	toasts.push({ id, message, type });
	if (duration > 0) {
		setTimeout(() => dismissToast(id), duration);
	}
	return id;
}

export function dismissToast(id: number): void {
	const idx = toasts.findIndex((t) => t.id === id);
	if (idx !== -1) toasts.splice(idx, 1);
}

// Convenience facade — preferred call site API.
export const toast = {
	success: (message: string, duration?: number) => addToast(message, 'success', duration),
	error: (message: string, duration?: number) => addToast(message, 'error', duration),
	info: (message: string, duration?: number) => addToast(message, 'info', duration),
};
