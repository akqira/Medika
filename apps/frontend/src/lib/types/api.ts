export interface PagedResult<T> {
	items: T[];
	totalCount: number;
	page: number;
	pageSize: number;
	totalPages: number;
	hasNextPage: boolean;
	hasPreviousPage: boolean;
}

export interface PatientSummary {
	id: string;
	firstName: string;
	lastName: string;
	age: number;
	gender: 'M' | 'F';
	phone: string;
	bloodGroup?: string;
	lastVisitAt?: string;
	allergyCount: number;
}

export interface AppointmentSlot {
	id: string;
	patientId: string;
	patientName: string;
	patientPhone: string;
	time: string;
	durationMinutes: number;
	reason: string;
	status: 'Pending' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled' | 'NoShow';
	type: string;
	patientBloodGroup?: string;
	patientAllergies: string[];
}

export interface FinancialSummary {
	totalIncome: number;
	totalCharges: number;
	netIncome: number;
	paidInvoices: number;
	pendingInvoices: number;
	pendingAmount: number;
	monthlyTrend: { month: string; amount: number }[];
	breakdownByType: { label: string; amount: number; percentage: number }[];
}
