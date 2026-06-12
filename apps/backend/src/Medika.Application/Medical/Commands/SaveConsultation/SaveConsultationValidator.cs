using FastEndpoints;
using FluentValidation;

namespace Medika.Application.Medical.Commands.SaveConsultation;

public class SaveConsultationValidator : Validator<SaveConsultationCommand>
{
    public SaveConsultationValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Tariff).GreaterThan(0);
        RuleFor(x => x.Prescription).NotNull();
    }
}
