using System.Text.RegularExpressions;

namespace Medika.Application.Common.Validation;

/// <summary>
/// Centralized Algerian phone validation (issue #124). One rule, shared by every
/// phone field's FluentValidation validator (patient create/update, cabinet) and
/// mirrored client-side by the frontend <c>$lib/phone</c> helper.
///
/// Mobile: 0[5-7] + 10 digits (05 Ooredoo, 06 Mobilis, 07 Djezzy).
/// Fixe:   0[1-4] + 9 digits  (indicatif régional, ex. 021 23 45 67 — Alger).
/// Spaces are stripped before matching.
/// </summary>
public static partial class AlgerianPhone
{
    public const string ErrorMessage =
        "Numéro algérien invalide — mobile (0555 12 34 56) ou fixe (021 23 45 67)";

    [GeneratedRegex(@"^0(?:[5-7]\d{8}|[1-4]\d{7})$")]
    private static partial Regex Pattern();

    /// <summary>True for a valid Algerian mobile or landline number (spaces ignored).</summary>
    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Pattern().IsMatch(value.Replace(" ", ""));
}
