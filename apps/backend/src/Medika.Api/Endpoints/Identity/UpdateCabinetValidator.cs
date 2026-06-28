using FastEndpoints;
using FluentValidation;
using Medika.Application.Common.Validation;

namespace Medika.Api.Endpoints.Identity;

public class UpdateCabinetValidator : Validator<UpdateCabinetRequest>
{
    // Shared Algerian phone rule (#124): mobile (05/06/07, 10 digits) or fixe (0[1-4], 9 digits).
    public UpdateCabinetValidator()
    {
        RuleFor(x => x.CabinetPhone)
            .Must(AlgerianPhone.IsValid)
            .WithMessage(AlgerianPhone.ErrorMessage)
            .When(x => !string.IsNullOrWhiteSpace(x.CabinetPhone));
    }
}
