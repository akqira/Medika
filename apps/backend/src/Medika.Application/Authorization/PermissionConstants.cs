namespace Medika.Application.Authorization;

/// <summary>
/// Centralised constants for every application permission (eGestion-style RBAC, ported to
/// Medika's cabinet-scoped domain). Permission strings are the contract between the
/// <c>permissions</c> JWT claim, the FastEndpoints <c>Permissions(...)</c> ACL guards, and
/// the front-end gating — so they must never be free-typed as magic strings.
/// </summary>
public static class PermissionConstants
{
    /// <summary>Patient registry.</summary>
    public static class Patients
    {
        public const string View = "patients_can_view";
        public const string Create = "patients_can_create";
        public const string Edit = "patients_can_edit";
        public const string Delete = "patients_can_delete";
    }

    /// <summary>Clinical consultations (doctor-centric).</summary>
    public static class Consultations
    {
        public const string View = "consultations_can_view";          // metadata-only for non-doctors (ADR-002)
        public const string Manage = "consultations_can_manage";       // create / edit / finalize
        public const string Prescribe = "consultations_can_prescribe"; // generate prescription PDF
    }

    /// <summary>Agenda / appointments.</summary>
    public static class Scheduling
    {
        public const string View = "scheduling_can_view";
        public const string Manage = "scheduling_can_manage"; // book / confirm / cancel / no-show
    }

    /// <summary>Finance — invoices, charges, acts, summary.</summary>
    public static class Finance
    {
        public const string ViewInvoices = "finance_can_view_invoices";
        public const string ManageInvoices = "finance_can_manage_invoices"; // mark paid
        public const string ViewSummary = "finance_can_view_summary";
        public const string ManageCharges = "finance_can_manage_charges";
        public const string ManageActs = "finance_can_manage_acts";
    }

    /// <summary>Team / user administration (the cabinet-admin surface).</summary>
    public static class Users
    {
        public const string View = "users_can_view";
        public const string Add = "users_can_add";
        public const string ManagePermissions = "users_can_manage_permissions";
        public const string ResetPassword = "users_can_reset_password";
    }

    /// <summary>Cabinet settings (doctor profile, practice details).</summary>
    public static class Cabinet
    {
        public const string ManageSettings = "cabinet_can_manage_settings";
    }

    /// <summary>Every permission, used to grant a Doctor admin the full set and to drive UI metadata.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Patients.View, Patients.Create, Patients.Edit, Patients.Delete,
        Consultations.View, Consultations.Manage, Consultations.Prescribe,
        Scheduling.View, Scheduling.Manage,
        Finance.ViewInvoices, Finance.ManageInvoices, Finance.ViewSummary,
        Finance.ManageCharges, Finance.ManageActs,
        Users.View, Users.Add, Users.ManagePermissions, Users.ResetPassword,
        Cabinet.ManageSettings,
    ];

    /// <summary>
    /// Sensible starting permissions for a freshly created Secretary (front-desk staff):
    /// patient + agenda management and invoice handling, but no clinical, financial-reporting,
    /// or team-admin access. The doctor can customise from here.
    /// </summary>
    public static IReadOnlyList<string> DefaultSecretary { get; } =
    [
        Patients.View, Patients.Create, Patients.Edit,
        Consultations.View,
        Scheduling.View, Scheduling.Manage,
        Finance.ViewInvoices, Finance.ManageInvoices,
    ];

    private static readonly HashSet<string> Known = [.. All];

    /// <summary>Filters an arbitrary input set down to recognised permission strings (defence against tampering).</summary>
    public static IEnumerable<string> Sanitize(IEnumerable<string>? permissions) =>
        (permissions ?? []).Where(Known.Contains).Distinct();
}
