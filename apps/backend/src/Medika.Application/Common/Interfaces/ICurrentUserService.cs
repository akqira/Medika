namespace Medika.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }
    string CabinetId { get; }
    string UserName { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
