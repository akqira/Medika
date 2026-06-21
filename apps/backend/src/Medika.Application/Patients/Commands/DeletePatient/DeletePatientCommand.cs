using FastEndpoints;

namespace Medika.Application.Patients.Commands.DeletePatient;

// Id is bound from the {id} route segment by the endpoint.
public record DeletePatientCommand(string Id) : ICommand;
