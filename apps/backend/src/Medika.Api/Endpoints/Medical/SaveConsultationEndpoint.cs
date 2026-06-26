using FastEndpoints;
using Medika.Application.Medical.Commands.SaveConsultation;

namespace Medika.Api.Endpoints.Medical;

public class SaveConsultationEndpoint : Endpoint<SaveConsultationCommand, SavedConsultationResponse>
{
    public override void Configure()
    {
        Post("/api/consultations");
        Permissions(PermissionConstants.Consultations.Manage);
    }

    public override async Task HandleAsync(SaveConsultationCommand req, CancellationToken ct)
    {
        var id = await req.ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new SavedConsultationResponse(id), 201, null, ct);
    }
}

public record SavedConsultationResponse(string ConsultationId);
