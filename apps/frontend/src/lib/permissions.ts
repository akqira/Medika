// Permission string constants — must stay in lock-step with the backend
// PermissionConstants (Medika.Application.Authorization). These are the keys carried in the
// JWT `permissions` claim and in the session cookie, and used to gate UI.

export const PERMISSIONS = {
	patients: {
		view: 'patients_can_view',
		create: 'patients_can_create',
		edit: 'patients_can_edit',
		delete: 'patients_can_delete'
	},
	consultations: {
		view: 'consultations_can_view',
		manage: 'consultations_can_manage',
		prescribe: 'consultations_can_prescribe'
	},
	scheduling: {
		view: 'scheduling_can_view',
		manage: 'scheduling_can_manage'
	},
	finance: {
		viewInvoices: 'finance_can_view_invoices',
		manageInvoices: 'finance_can_manage_invoices',
		viewSummary: 'finance_can_view_summary',
		manageCharges: 'finance_can_manage_charges',
		manageActs: 'finance_can_manage_acts'
	},
	users: {
		view: 'users_can_view',
		add: 'users_can_add',
		managePermissions: 'users_can_manage_permissions',
		resetPassword: 'users_can_reset_password'
	},
	cabinet: {
		manageSettings: 'cabinet_can_manage_settings'
	}
} as const;

/** True when the permission set contains the given key. */
export function can(permissions: string[] | undefined, key: string): boolean {
	return !!permissions && permissions.includes(key);
}

/**
 * Sensible starting permissions pre-checked when adding a secretary — mirrors the backend
 * PermissionConstants.DefaultSecretary (front-desk: patients + agenda + invoicing).
 */
export const DEFAULT_SECRETARY: string[] = [
	PERMISSIONS.patients.view,
	PERMISSIONS.patients.create,
	PERMISSIONS.patients.edit,
	PERMISSIONS.consultations.view,
	PERMISSIONS.scheduling.view,
	PERMISSIONS.scheduling.manage,
	PERMISSIONS.finance.viewInvoices,
	PERMISSIONS.finance.manageInvoices
];
