using FastEndpoints;
using Medika.Application.Identity.Commands.UpdateCabinet;

namespace Medika.Api.Endpoints.Identity;

public class UpdateCabinetRequest
{
    public string? CabinetName { get; init; }
    public string? Specialty { get; init; }
    public string? RppsNumber { get; init; }
    public string? CabinetAddress { get; init; }
    public string? CabinetCity { get; init; }
    public string? CabinetWilaya { get; init; }
    public string? CabinetPhone { get; init; }
}

public class UpdateCabinetEndpoint : Endpoint<UpdateCabinetRequest>
{
    public override void Configure()
    {
        Patch("/api/profile/cabinet");
        Roles("Doctor");
    }

    public override async Task HandleAsync(UpdateCabinetRequest req, CancellationToken ct)
    {
        var cmd = new UpdateCabinetCommand(
            req.CabinetName, req.Specialty, req.RppsNumber,
            req.CabinetAddress, req.CabinetCity, req.CabinetWilaya, req.CabinetPhone);

        await cmd.ExecuteAsync(ct);
        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
