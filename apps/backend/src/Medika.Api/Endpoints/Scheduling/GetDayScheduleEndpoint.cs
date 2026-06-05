using FastEndpoints;
using Medika.Application.Scheduling.Queries.GetDaySchedule;

namespace Medika.Api.Endpoints.Scheduling;

public class GetDayScheduleEndpoint : Endpoint<GetDayScheduleRequest, IReadOnlyList<AppointmentSlot>>
{
    public override void Configure()
    {
        Get("/api/schedule/{date}");
        Roles("Doctor", "Receptionist");
    }

    public override async Task<IReadOnlyList<AppointmentSlot>> ExecuteAsync(GetDayScheduleRequest req, CancellationToken ct)
    {
        return await new GetDayScheduleQuery(req.Date).ExecuteAsync(ct);
    }
}

public record GetDayScheduleRequest([property: RouteParam] string Date);
