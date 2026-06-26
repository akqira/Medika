using FastEndpoints;
using FluentValidation;

namespace Medika.Api.Endpoints.Identity;

public class UpdateCabinetValidator : Validator<UpdateCabinetRequest>
{
    // Algerian phone: mobile (05/06/07) or fixed (02/03/04), 10 digits total.
    public UpdateCabinetValidator()
    {
        RuleFor(x => x.CabinetPhone)
            .Matches(@"^0[2-7]\d{8}$")
            .WithMessage("Numéro algérien invalide — ex : 0550 12 34 56 ou 023 45 67 89")
            .When(x => !string.IsNullOrWhiteSpace(x.CabinetPhone));
    }
}
