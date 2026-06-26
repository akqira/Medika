using FastEndpoints;
using Medika.Application.Authorization;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Identity;

namespace Medika.Application.Identity.Commands.Login;

public class LoginHandler(IUserRepository users, IJwtTokenGenerator jwt, IAuditService audit, IPasswordHasher hasher)
    : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> ExecuteAsync(LoginCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(cmd.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (!hasher.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.RecordLogin();
        await users.UpdateAsync(user, ct);

        await audit.LogAsync("Login", "User", user.Id.ToString(), ct: ct);

        var token = jwt.Generate(user);
        return new LoginResult(token, user.Id.ToString(), user.Role.ToString(),
            $"{user.FirstName} {user.LastName}", PermissionResolver.Resolve(user));
    }
}
