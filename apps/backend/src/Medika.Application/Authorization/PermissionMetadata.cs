namespace Medika.Application.Authorization;

public record PermissionItem(string Key, string Label, string Description);
public record PermissionCategory(string Key, string Label, string Icon, IReadOnlyList<PermissionItem> Permissions);

/// <summary>
/// French-labelled, category-grouped description of every permission — drives the checkbox grid
/// on the "Équipe" screen so the doctor can customise a secretary's access. Pure metadata; the
/// keys are the same strings used in the JWT claims and <see cref="PermissionConstants"/>.
/// </summary>
public static class PermissionMetadata
{
    public static IReadOnlyList<PermissionCategory> Categories { get; } =
    [
        new("patients", "Patients", "users",
        [
            new(PermissionConstants.Patients.View,   "Consulter les patients",   "Voir la liste et les fiches patients"),
            new(PermissionConstants.Patients.Create, "Ajouter des patients",     "Créer de nouvelles fiches patients"),
            new(PermissionConstants.Patients.Edit,   "Modifier des patients",    "Mettre à jour les informations patients"),
            new(PermissionConstants.Patients.Delete, "Supprimer des patients",   "Supprimer une fiche patient sans historique"),
        ]),
        new("scheduling", "Agenda", "calendar",
        [
            new(PermissionConstants.Scheduling.View,   "Consulter l'agenda",     "Voir les rendez-vous"),
            new(PermissionConstants.Scheduling.Manage, "Gérer l'agenda",         "Planifier, confirmer, annuler et marquer les absences"),
        ]),
        new("consultations", "Consultations", "clipboard",
        [
            new(PermissionConstants.Consultations.View,      "Historique des consultations", "Voir qu'une consultation a eu lieu (métadonnées)"),
            new(PermissionConstants.Consultations.Manage,    "Gérer les consultations",      "Créer, modifier et clôturer les consultations (accès clinique)"),
            new(PermissionConstants.Consultations.Prescribe, "Ordonnances",                  "Générer les ordonnances en PDF"),
        ]),
        new("finance", "Finances", "barchart",
        [
            new(PermissionConstants.Finance.ViewInvoices,   "Consulter les factures", "Voir les factures et le catalogue d'actes"),
            new(PermissionConstants.Finance.ManageInvoices, "Encaisser les factures", "Marquer les factures comme payées"),
            new(PermissionConstants.Finance.ViewSummary,    "Bilan financier",        "Accéder au tableau de bord financier"),
            new(PermissionConstants.Finance.ManageCharges,  "Gérer les charges",      "Saisir et supprimer les charges du cabinet"),
            new(PermissionConstants.Finance.ManageActs,     "Gérer les actes",        "Créer et supprimer les actes facturables"),
        ]),
        new("users", "Équipe", "shieldCheck",
        [
            new(PermissionConstants.Users.View,              "Consulter l'équipe",        "Voir les membres du cabinet"),
            new(PermissionConstants.Users.Add,               "Ajouter des membres",       "Créer des comptes secrétaire"),
            new(PermissionConstants.Users.ManagePermissions, "Gérer les permissions",     "Modifier les accès et activer/désactiver les comptes"),
            new(PermissionConstants.Users.ResetPassword,     "Réinitialiser les mots de passe", "Envoyer un lien de réinitialisation à un membre"),
        ]),
        new("cabinet", "Cabinet", "settings",
        [
            new(PermissionConstants.Cabinet.ManageSettings, "Paramètres du cabinet", "Modifier le profil et les informations du cabinet"),
        ]),
    ];
}
