using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.Register;

public class RegisterHandler(IUserRepository users, IAuditService audit, IPasswordHasher hasher)
    : ICommandHandler<RegisterCommand, string>
{
    public async Task<string> ExecuteAsync(RegisterCommand cmd, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(cmd.Email, ct))
            throw new InvalidOperationException($"Email {cmd.Email} is already registered.");

        var hash = hasher.Hash(cmd.Password);
        var user = User.Create(cmd.Email, hash, cmd.FirstName, cmd.LastName,
            cmd.Role, cmd.Specialty, cmd.OrderNumber);

        await users.AddAsync(user, ct);
        await audit.LogAsync("Register", "User", user.Id.ToString(), after: new { user.Email, user.Role }, ct: ct);

        return user.Id.ToString();
    }
}
