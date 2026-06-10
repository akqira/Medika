using FastEndpoints;
using FluentValidation;

namespace Medika.Application.Scheduling.Commands.BookAppointment;

public class BookAppointmentValidator : Validator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Date)
            .NotEmpty()
            .Must(d => DateOnly.TryParse(d, out _))
            .WithMessage("Date must be a valid date (yyyy-MM-dd).");
        RuleFor(x => x.Time)
            .NotEmpty()
            .Must(t => TimeOnly.TryParse(t, out _))
            .WithMessage("Time must be a valid time (HH:mm).");
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Type).NotEmpty();
    }
}
