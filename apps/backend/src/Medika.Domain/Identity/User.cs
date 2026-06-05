using Medika.Domain.Common;
using Medika.Domain.Identity.Events;

namespace Medika.Domain.Identity;

public sealed class User : AggregateRoot<UserId>
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Role Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? LastLoginAt { get; private set; }

    // Doctor-specific (null for receptionist/patient)
    public string? Specialty { get; private set; }
    public string? OrderNumber { get; private set; }

    // Patient-specific — links this User to the PatientId
    public string? LinkedPatientId { get; private set; }

    private User() { }

    public static User Create(
        string email, string passwordHash,
        string firstName, string lastName,
        Role role,
        string? specialty = null,
        string? orderNumber = null)
    {
        var user = new User
        {
            Id = UserId.New(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Specialty = specialty,
            OrderNumber = orderNumber,
        };
        user.Raise(new UserCreated(user.Id, user.Email, user.Role));
        return user;
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void LinkToPatient(string patientId) => LinkedPatientId = patientId;

    public void Deactivate() => IsActive = false;
}
