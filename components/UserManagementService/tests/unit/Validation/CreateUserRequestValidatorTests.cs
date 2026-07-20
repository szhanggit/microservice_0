using FluentAssertions;
using Shared.Contracts.Constants;
using Shared.Protos.UserManagement;
using UserManagementService.Validation;

namespace UserManagementService.Tests.Unit.Validation;

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _sut = new();

    [Fact]
    public void Validate_WithValidRequest_ReturnsValidOutcome()
    {
        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Doe", "jane@example.com")]
    [InlineData("Jane", "", "jane@example.com")]
    [InlineData("Jane", "Doe", "")]
    public void Validate_WithMissingRequiredField_ReturnsInvalidOutcome(string firstName, string lastName, string email)
    {
        var request = new CreateUserRequest { FirstName = firstName, LastName = lastName, Email = email };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithFirstNameTooLong_ReturnsInvalidOutcome()
    {
        var request = new CreateUserRequest
        {
            FirstName = new string('a', UserValidationConstants.FirstNameMaxLength + 1),
            LastName = "Doe",
            Email = "jane@example.com",
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("FirstName"));
    }

    [Fact]
    public void Validate_WithEmailTooLong_ReturnsInvalidOutcome()
    {
        var longLocalPart = new string('a', UserValidationConstants.EmailMaxLength);
        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = $"{longLocalPart}@example.com" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Email"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@missing-local-part.com")]
    public void Validate_WithInvalidEmailFormat_ReturnsInvalidOutcome(string email)
    {
        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = email };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
