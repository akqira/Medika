using FastEndpoints;
using FluentValidation;
using Medika.Application.Common.Validation;

namespace Medika.Application.Patients.Commands.UpdatePatient;

public class UpdatePatientValidator : Validator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).NotEmpty()
            .Must(d => DateOnly.TryParse(d, out _)).WithMessage("Invalid date format. Use ISO: YYYY-MM-DD");
        RuleFor(x => x.Gender).NotEmpty().Must(g => g is "M" or "F").WithMessage("Gender must be M or F");
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20)
            .Must(AlgerianPhone.IsValid).WithMessage(AlgerianPhone.ErrorMessage);
        RuleFor(x => x.EmergencyContactPhone)
            .Must(AlgerianPhone.IsValid).WithMessage(AlgerianPhone.ErrorMessage)
            .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContactPhone));
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}
