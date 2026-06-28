using System.Text.RegularExpressions;
using FluentValidation;

namespace Medika.Api.Common.Validation;

/// <summary>
/// Shared FluentValidation rules. FluentValidation's built-in <c>.EmailAddress()</c> defaults to
/// ASP.NET-compatible mode, which only checks for a single <c>@</c> with text on both sides — so
/// non-deliverable values like <c>a@b</c> (no TLD) pass. <see cref="ValidEmail{T}"/> additionally
/// requires a dotted domain, matching the client-side <c>pattern</c> on the email inputs.
/// </summary>
public static class ValidationRuleExtensions
{
    // user@domain.tld — no whitespace, exactly one @, and at least one dot in the domain.
    private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Matches(EmailPattern, RegexOptions.None)
            .WithMessage("L'adresse email n'est pas valide.");
}
