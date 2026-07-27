using FluentAssertions;
using Shared.Protos.UserManagement;
using UserManagementService.Validation;

namespace UserManagementService.Tests.Unit.Validation;

public sealed class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _sut = new();

    [Fact]
    public void Validate_WithValidRequest_ReturnsValidOutcome()
    {
        var request = new UpdateUserRequest { UserId = Guid.NewGuid().ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void Validate_WithInvalidUserId_ReturnsInvalidOutcome(string userId)
    {
        var request = new UpdateUserRequest { UserId = userId, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithMissingLastName_ReturnsInvalidOutcome()
    {
        var request = new UpdateUserRequest { UserId = Guid.NewGuid().ToString(), FirstName = "Jane", LastName = "", Email = "jane@example.com" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("LastName"));
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ReturnsInvalidOutcome()
    {
        var request = new UpdateUserRequest { UserId = Guid.NewGuid().ToString(), FirstName = "Jane", LastName = "Doe", Email = "not-an-email" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithMultipleInvalidFields_ReturnsAllErrors()
    {
        var request = new UpdateUserRequest { UserId = "not-a-guid", FirstName = "", LastName = "", Email = "not-an-email" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }
}
