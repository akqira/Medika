using Medika.Domain.Identity;

namespace Medika.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
