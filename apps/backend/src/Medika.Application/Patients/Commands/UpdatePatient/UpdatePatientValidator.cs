using FastEndpoints;
using FluentValidation;

namespace Medika.Application.Patients.Commands.UpdatePatient;

public class UpdatePatientValidator : Validator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).NotEmpty()
            .Must(d => DateOnly.TryParse(d, out _)).WithMessage("Invalid date format. Use ISO: YYYY-MM-DD");
        RuleFor(x => x.Gender).NotEmpty().Must(g => g is "M" or "F").WithMessage("Gender must be M or F");
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20)
            .Matches(@"^0[5-7]\d{8}$").WithMessage("Format invalide — ex: 0555 12 34 56");
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Nss)
            .Matches(@"^\d{15}$").WithMessage("Le NSS doit contenir exactement 15 chiffres")
            .When(x => !string.IsNullOrEmpty(x.Nss));
    }
}
