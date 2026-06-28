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

export interface PatientDetail {
	id: string;
	firstName: string;
	lastName: string;
	dateOfBirth?: string;
	age: number;
	gender: 'M' | 'F';
	phone: string;
	email?: string;
	address?: string;
	nss?: string;
	bloodGroup?: string;
	wilaya?: string;
	emergencyContactName?: string;
	emergencyContactPhone?: string;
	insuranceProvider?: string;
	mutualInsurance?: string;
	currentTreatment?: string;
	allergies: string[];
	medicalHistory: string[];
	lastVisitAt?: string;
	createdAt: string;
}

export interface PatientInvoice {
	id: string;
	number: string;
	consultationId: string;
	amount: number;
	status: 'Pending' | 'Paid' | 'Cancelled';
	paymentMethod?: 'Cash' | 'BankTransfer' | 'Check' | 'Other';
	issuedAt: string;
	paidAt?: string;
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

export interface PrescriptionLine {
	medication: string;
	dosage: string;
	frequency: string;
	duration: string;
}

export interface VitalSigns {
	bloodPressure?: string;
	pulseRate?: string;
	temperature?: string;
	weight?: string;
	height?: string;
	spO2?: string;
}

export interface ConsultationSummary {
	consultationId: string;
	date: string;
	reason: string;
	diagnosis?: string;
	tariff: number;
	isFinalized: boolean;
	prescriptionCount: number;
	appointmentId?: string;
}

export interface ConsultationDetail extends ConsultationSummary {
	patientId: string;
	doctorId: string;
	clinicalExam?: string;
	notes?: string;
	vitalSigns?: VitalSigns;
	prescription: PrescriptionLine[];
}

export interface AppointmentBooking {
	id: string;
	patientId: string;
	patientName: string;
	date: string;
	time: string;
	durationMinutes: number;
	type: string;
	reason?: string;
	status: 'Pending' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled' | 'NoShow';
}

export interface Charge {
	id: string;
	category: string;
	description: string;
	amount: number;
	date: string;
}

export interface Act {
	id: string;
	name: string;
	tariff: number;
}
