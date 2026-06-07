using FastEndpoints;

namespace Medika.Application.Identity.Commands.UpdateCabinet;

public record UpdateCabinetCommand(
    string? CabinetName,
    string? Specialty,
    string? RppsNumber,
    string? CabinetAddress,
    string? CabinetCity,
    string? CabinetWilaya,
    string? CabinetPhone) : ICommand;
