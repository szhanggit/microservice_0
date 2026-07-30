using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Common.Exceptions;
using Shared.Contracts.Requests;
using UserManagementGateway.Services;
using UserManagementGateway.Tests.Unit.TestUtilities;
using UserManagementGateway.Validation;
using ManagementProto = Shared.Protos.UserManagement;
using CommonProto = Shared.Protos.Common;

namespace UserManagementGateway.Tests.Unit.Services;

public sealed class UserServiceTests
{
    private readonly Mock<ManagementProto.UserManagementGrpcService.UserManagementGrpcServiceClient> _managementClient = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_managementClient.Object, new CreateUserRequestValidator(), new UpdateUserRequestValidator(), Mock.Of<ILogger<UserService>>());
    }

    [Fact]
    public async Task CreateUserAsync_WithValidRequest_ReturnsCreatedUser()
    {
        var userId = Guid.NewGuid();
        _managementClient
            .Setup(c => c.CreateUserAsync(It.IsAny<ManagementProto.CreateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new ManagementProto.UserReply
            {
                User = new CommonProto.UserProto { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" },
            }));

        var request = new CreateUserRequest("Jane", "Doe", "jane@example.com");

        var result = await _sut.CreateUserAsync(request, CancellationToken.None);

        result.UserId.Should().Be(userId);
        result.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task CreateUserAsync_WithMissingFirstName_ThrowsValidationException()
    {
        var request = new CreateUserRequest("", "Doe", "jane@example.com");

        var act = () => _sut.CreateUserAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _managementClient.Verify(
            c => c.CreateUserAsync(It.IsAny<ManagementProto.CreateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidEmail_ThrowsValidationException()
    {
        var request = new CreateUserRequest("Jane", "Doe", "not-an-email");

        var act = () => _sut.CreateUserAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateUserAsync_WhenManagementReturnsAlreadyExists_ThrowsDuplicateEmailException()
    {
        _managementClient
            .Setup(c => c.CreateUserAsync(It.IsAny<ManagementProto.CreateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<ManagementProto.UserReply>(
                new RpcException(new Status(StatusCode.AlreadyExists, "duplicate"))));

        var request = new CreateUserRequest("Jane", "Doe", "jane@example.com");

        var act = () => _sut.CreateUserAsync(request, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DuplicateEmailException>();
        exception.Which.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsUser()
    {
        var userId = Guid.NewGuid();
        _managementClient
            .Setup(c => c.GetUserByIdAsync(It.IsAny<ManagementProto.GetUserByIdRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new ManagementProto.UserReply
            {
                User = new CommonProto.UserProto { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" },
            }));

        var result = await _sut.GetUserByIdAsync(userId, CancellationToken.None);

        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenManagementReturnsNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _managementClient
            .Setup(c => c.GetUserByIdAsync(It.IsAny<ManagementProto.GetUserByIdRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<ManagementProto.UserReply>(
                new RpcException(new Status(StatusCode.NotFound, $"User with id '{userId}' was not found."))));

        var act = () => _sut.GetUserByIdAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUserAsync_WithValidRequest_ReturnsUpdatedUser()
    {
        var userId = Guid.NewGuid();
        _managementClient
            .Setup(c => c.UpdateUserAsync(It.IsAny<ManagementProto.UpdateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new ManagementProto.UserReply
            {
                User = new CommonProto.UserProto { UserId = userId.ToString(), FirstName = "Janet", LastName = "Doe", Email = "janet@example.com" },
            }));

        var request = new UpdateUserRequest("Janet", "Doe", "janet@example.com");

        var result = await _sut.UpdateUserAsync(userId, request, CancellationToken.None);

        result.FirstName.Should().Be("Janet");
    }

    [Fact]
    public async Task UpdateUserAsync_WithMissingField_ThrowsValidationException()
    {
        var request = new UpdateUserRequest("", "Doe", "jane@example.com");

        var act = () => _sut.UpdateUserAsync(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateUserAsync_WhenManagementReturnsAlreadyExists_ThrowsDuplicateEmailException()
    {
        _managementClient
            .Setup(c => c.UpdateUserAsync(It.IsAny<ManagementProto.UpdateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<ManagementProto.UserReply>(
                new RpcException(new Status(StatusCode.AlreadyExists, "duplicate"))));

        var request = new UpdateUserRequest("Jane", "Doe", "taken@example.com");

        var act = () => _sut.UpdateUserAsync(Guid.NewGuid(), request, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DuplicateEmailException>();
        exception.Which.Email.Should().Be("taken@example.com");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenManagementReturnsNotFound_ThrowsNotFoundException()
    {
        _managementClient
            .Setup(c => c.UpdateUserAsync(It.IsAny<ManagementProto.UpdateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<ManagementProto.UserReply>(
                new RpcException(new Status(StatusCode.NotFound, "not found"))));

        var request = new UpdateUserRequest("Jane", "Doe", "jane@example.com");

        var act = () => _sut.UpdateUserAsync(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteUserAsync_WithExistingUser_Succeeds()
    {
        _managementClient
            .Setup(c => c.DeleteUserAsync(It.IsAny<ManagementProto.DeleteUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new ManagementProto.DeleteUserReply { Success = true }));

        var act = () => _sut.DeleteUserAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenManagementReturnsNotFound_ThrowsNotFoundException()
    {
        _managementClient
            .Setup(c => c.DeleteUserAsync(It.IsAny<ManagementProto.DeleteUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<ManagementProto.DeleteUserReply>(
                new RpcException(new Status(StatusCode.NotFound, "not found"))));

        var act = () => _sut.DeleteUserAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SearchUsersAsync_ReturnsMappedUsers()
    {
        var repoReply = new ManagementProto.SearchUsersReply();
        repoReply.Users.Add(new CommonProto.UserProto { UserId = Guid.NewGuid().ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" });
        _managementClient
            .Setup(c => c.SearchUsersAsync(It.IsAny<ManagementProto.SearchUsersRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(repoReply));

        var result = await _sut.SearchUsersAsync("Doe", CancellationToken.None);

        result.Users.Should().ContainSingle(u => u.LastName == "Doe");
    }

    [Fact]
    public async Task SearchUsersAsync_WithNullName_SendsEmptyStringToManagement()
    {
        _managementClient
            .Setup(c => c.SearchUsersAsync(
                It.Is<ManagementProto.SearchUsersRequest>(r => r.Name == string.Empty),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new ManagementProto.SearchUsersReply()));

        await _sut.SearchUsersAsync(null, CancellationToken.None);

        _managementClient.Verify(
            c => c.SearchUsersAsync(
                It.Is<ManagementProto.SearchUsersRequest>(r => r.Name == string.Empty),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
