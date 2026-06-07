using FastEndpoints;

namespace Medika.Application.Identity.Queries.GetProfile;

public record GetProfileQuery : ICommand<ProfileResult>;

public record ProfileResult(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Specialty,
    string? RppsNumber,
    string? CabinetName,
    string? CabinetAddress,
    string? CabinetCity,
    string? CabinetWilaya,
    string? CabinetPhone,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
