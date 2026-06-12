using Medika.Domain.Common;
using Medika.Domain.Scheduling.Events;

namespace Medika.Domain.Scheduling;

public sealed class Appointment : AggregateRoot<AppointmentId>
{
    public string CabinetId { get; private set; } = null!;
    public string PatientId { get; private set; } = null!;
    public string DoctorId { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public TimeOnly Time { get; private set; }
    public int DurationMinutes { get; private set; }
    public string Reason { get; private set; } = null!;
    public AppointmentStatus Status { get; private set; }
    public AppointmentType Type { get; private set; }
    public string? ConsultationId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private Appointment() { }

    public static Appointment Book(
        string cabinetId,
        string patientId, string doctorId,
        DateOnly date, TimeOnly time,
        int durationMinutes, string reason,
        AppointmentType type = AppointmentType.FollowUp)
    {
        var appt = new Appointment
        {
            Id = AppointmentId.New(),
            CabinetId = cabinetId,
            PatientId = patientId,
            DoctorId = doctorId,
            Date = date,
            Time = time,
            DurationMinutes = durationMinutes,
            Reason = reason,
            Type = type,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        appt.Raise(new AppointmentBooked(appt.Id, appt.PatientId, appt.Date, appt.Time));
        return appt;
    }

    public void Confirm()
    {
        EnsureStatus(AppointmentStatus.Pending);
        Status = AppointmentStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        EnsureStatus(AppointmentStatus.Pending, AppointmentStatus.Confirmed);
        Status = AppointmentStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(string consultationId)
    {
        EnsureStatus(AppointmentStatus.InProgress);
        Status = AppointmentStatus.Completed;
        ConsultationId = consultationId;
        UpdatedAt = DateTime.UtcNow;
        Raise(new AppointmentCompleted(Id, PatientId, consultationId));
    }

    public void Cancel(string? reason = null)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel appointment in status {Status}.");
        Status = AppointmentStatus.Cancelled;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkNoShow()
    {
        EnsureStatus(AppointmentStatus.Confirmed, AppointmentStatus.Pending);
        Status = AppointmentStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateOnly date, TimeOnly time, int durationMinutes)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Cannot reschedule a completed or cancelled appointment.");
        Date = date;
        Time = time;
        DurationMinutes = durationMinutes;
        Status = AppointmentStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(params AppointmentStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOperationException(
                $"Operation not allowed in status {Status}. Expected: {string.Join(", ", allowed)}.");
    }
}
