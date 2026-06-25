using FastEndpoints;
using Medika.Application.Scheduling.Commands.BookAppointment;

namespace Medika.Api.Endpoints.Scheduling;

public class BookAppointmentEndpoint : Endpoint<BookAppointmentCommand, BookedResponse>
{
    public override void Configure()
    {
        Post("/api/appointments");
        Permissions(PermissionConstants.Scheduling.Manage);
    }

    public override async Task HandleAsync(BookAppointmentCommand req, CancellationToken ct)
    {
        var id = await req.ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new BookedResponse(id), 201, null, ct);
    }
}

public record BookedResponse(string AppointmentId);
