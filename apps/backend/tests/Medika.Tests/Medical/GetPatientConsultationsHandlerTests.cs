using System.Text.Json;
using Medika.Application.Medical.Queries.GetPatientConsultations;
using Medika.Domain.Medical;
using Medika.Tests.Fakes;
using Xunit;

namespace Medika.Tests.Medical;

/// <summary>
/// ADR-002 — the consultations query is role-shaped. Doctors get the full clinical
/// summary; receptionists (and any non-Doctor role) get metadata only, with clinical
/// fields physically absent from the payload.
/// </summary>
public class GetPatientConsultationsHandlerTests
{
    private const string Cabinet = "cabinet-1";
    private const string Patient = "patient-1";

    private static GetPatientConsultationsHandler HandlerFor(string role, params Consultation[] seed)
    {
        var repo = new FakeConsultationRepository(seed);
        var user = new FakeCurrentUserService { Role = role, CabinetId = Cabinet };
        return new GetPatientConsultationsHandler(repo, user);
    }

    private static GetPatientConsultationsQuery Query() => new(Patient, Page: 1, PageSize: 20);

    [Fact]
    public async Task Doctor_gets_full_summary_with_diagnosis()
    {
        var consult = ConsultationFactory.WithDiagnosis(Cabinet, Patient, diagnosis: "Grippe");
        var handler = HandlerFor("Doctor", consult);

        var result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        var summary = Assert.IsType<ConsultationSummary>(item);
        Assert.Equal("Grippe", summary.Diagnosis);
        Assert.Equal("Fièvre", summary.Reason);
        Assert.Equal(2000m, summary.Tariff);
    }

    [Fact]
    public async Task Receptionist_gets_metadata_only_no_clinical_fields()
    {
        var consult = ConsultationFactory.WithDiagnosis(Cabinet, Patient, diagnosis: "Grippe");
        var handler = HandlerFor("Receptionist", consult);

        var result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        var meta = Assert.IsType<ConsultationMetadata>(item);
        Assert.Equal(consult.Id.ToString(), meta.ConsultationId);
        Assert.True(meta.IsFinalized);
        // ConsultationMetadata has no Diagnosis/Reason/Tariff property — compile-time guarantee.
        Assert.IsNotType<ConsultationSummary>(item);
    }

    [Fact]
    public async Task Receptionist_payload_does_not_serialize_diagnosis_reason_or_tariff()
    {
        var consult = ConsultationFactory.WithDiagnosis(Cabinet, Patient, reason: "Douleur", diagnosis: "Migraine");
        var handler = HandlerFor("Receptionist", consult);

        var result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        // Mirror the API's camelCase serialization (Program.cs).
        var json = JsonSerializer.Serialize(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.DoesNotContain("Migraine", json);
        Assert.DoesNotContain("Douleur", json);
        Assert.DoesNotContain("diagnosis", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reason", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tariff", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_role_fails_safe_to_metadata()
    {
        var consult = ConsultationFactory.WithDiagnosis(Cabinet, Patient);
        var handler = HandlerFor("Janitor", consult);

        var result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        Assert.IsType<ConsultationMetadata>(Assert.Single(result.Items));
    }

    [Fact]
    public async Task Empty_cabinet_claim_is_rejected()
    {
        var repo = new FakeConsultationRepository([]);
        var user = new FakeCurrentUserService { Role = "Doctor", CabinetId = "" };
        var handler = new GetPatientConsultationsHandler(repo, user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.ExecuteAsync(Query(), CancellationToken.None));
    }

    [Fact]
    public async Task Query_is_scoped_to_the_jwt_cabinet_and_excludes_other_cabinets()
    {
        var mine = ConsultationFactory.WithDiagnosis(Cabinet, Patient);
        var otherCabinet = ConsultationFactory.WithDiagnosis("cabinet-2", Patient);
        var repo = new FakeConsultationRepository([mine, otherCabinet]);
        var user = new FakeCurrentUserService { Role = "Doctor", CabinetId = Cabinet };
        var handler = new GetPatientConsultationsHandler(repo, user);

        var result = await handler.ExecuteAsync(Query(), CancellationToken.None);

        Assert.Equal(Cabinet, repo.LastQueriedCabinetId);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
