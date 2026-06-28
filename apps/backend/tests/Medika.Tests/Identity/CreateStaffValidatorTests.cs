using FluentValidation.TestHelper;
using Medika.Api.Endpoints.Identity.Users;
using Xunit;

namespace Medika.Tests.Identity;

public class CreateStaffValidatorTests
{
    private readonly CreateStaffValidator _validator = new();

    private static CreateStaffRequest Request(string email) => new()
    {
        Email = email,
        Password = "password1",
        FirstName = "Test",
        LastName = "User"
    };

    [Theory]
    [InlineData("a@b")]            // no TLD — the bug in issue #100
    [InlineData("a@b.")]          // empty TLD
    [InlineData("plainaddress")]  // no @
    [InlineData("@no-local.com")] // no local part
    [InlineData("spaces in@x.com")]
    [InlineData("")]
    public void Rejects_malformed_emails(string email)
    {
        _validator.TestValidate(Request(email)).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("nom@cabinet.fr")]
    [InlineData("test.user@sub.domain.com")]
    public void Accepts_well_formed_emails(string email)
    {
        _validator.TestValidate(Request(email)).ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}
