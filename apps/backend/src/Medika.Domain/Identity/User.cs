using Medika.Domain.Common;
using Medika.Domain.Identity.Events;

namespace Medika.Domain.Identity;

public sealed class User : AggregateRoot<UserId>
{
    public string CabinetId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Role Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Doctor-specific (null for receptionist/patient)
    public string? Specialty { get; private set; }
    public string? OrderNumber { get; private set; }

    // Cabinet info (doctor's practice)
    public string? CabinetName { get; private set; }
    public string? CabinetAddress { get; private set; }
    public string? CabinetCity { get; private set; }
    public string? CabinetWilaya { get; private set; }
    public string? CabinetPhone { get; private set; }

    // Patient-specific — links this User to the PatientId
    public string? LinkedPatientId { get; private set; }

    // Password reset — single-use token (stored hashed) with an expiry.
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetExpiresAtUtc { get; private set; }

    private User() { }

    public static User Create(
        string email, string passwordHash,
        string firstName, string lastName,
        Role role,
        string? specialty = null,
        string? orderNumber = null,
        string? cabinetId = null)
    {
        var user = new User
        {
            Id = UserId.New(),
            // A new doctor founds a new cabinet by default; staff inherit the creator's cabinet.
            CabinetId = cabinetId ?? Guid.NewGuid().ToString("N"),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Specialty = specialty,
            OrderNumber = orderNumber,
        };
        user.Raise(new UserCreated(user.Id, user.Email, user.Role));
        return user;
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void LinkToPatient(string patientId) => LinkedPatientId = patientId;

    public void Deactivate() => IsActive = false;

    public void UpdateCabinet(
        string? cabinetName,
        string? specialty,
        string? orderNumber,
        string? cabinetAddress,
        string? cabinetCity,
        string? cabinetWilaya,
        string? cabinetPhone)
    {
        CabinetName = cabinetName;
        Specialty = specialty;
        OrderNumber = orderNumber;
        CabinetAddress = cabinetAddress;
        CabinetCity = cabinetCity;
        CabinetWilaya = cabinetWilaya;
        CabinetPhone = cabinetPhone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAccount(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        FirstName = firstName;
        LastName = lastName;
        Email = email.ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash)) throw new ArgumentException("Password hash is required.");
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Password reset ──

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("Reset token hash is required.");
        PasswordResetTokenHash = tokenHash;
        PasswordResetExpiresAtUtc = expiresAtUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasValidPasswordResetToken(DateTime nowUtc) =>
        PasswordResetTokenHash is not null
        && PasswordResetExpiresAtUtc is not null
        && PasswordResetExpiresAtUtc > nowUtc;

    // Sets the new password AND consumes the reset token (single-use).
    public void ResetPassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash)) throw new ArgumentException("Password hash is required.");
        PasswordHash = newPasswordHash;
        PasswordResetTokenHash = null;
        PasswordResetExpiresAtUtc = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
