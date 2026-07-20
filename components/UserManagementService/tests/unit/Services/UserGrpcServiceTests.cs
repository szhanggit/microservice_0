using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Protos.Common;
using Shared.Protos.UserManagement;
using UserManagementService.Services;
using UserManagementService.Tests.Unit.TestUtilities;
using UserManagementService.Validation;
using RepoProto = Shared.Protos.UserRepository;

namespace UserManagementService.Tests.Unit.Services;

public sealed class UserGrpcServiceTests
{
    private readonly Mock<RepoProto.UserRepositoryGrpcService.UserRepositoryGrpcServiceClient> _repositoryClient = new();
    private readonly UserGrpcService _sut;

    public UserGrpcServiceTests()
    {
        _sut = new UserGrpcService(
            _repositoryClient.Object,
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator(),
            Mock.Of<ILogger<UserGrpcService>>());
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedUser()
    {
        SetupEmailExists("jane@example.com", exists: false);
        var createdProto = new UserProto { UserId = Guid.NewGuid().ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
        _repositoryClient
            .Setup(c => c.CreateUserAsync(It.IsAny<RepoProto.CreateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new RepoProto.UserReply { User = createdProto }));

        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var reply = await _sut.CreateUser(request, TestServerCallContext.Create());

        reply.User.Email.Should().Be("jane@example.com");
        _repositoryClient.Verify(
            c => c.CreateUserAsync(It.IsAny<RepoProto.CreateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithMissingFirstName_ThrowsInvalidArgument()
    {
        var request = new CreateUserRequest { FirstName = "", LastName = "Doe", Email = "jane@example.com" };

        var act = () => _sut.CreateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
        _repositoryClient.Verify(
            c => c.EmailExistsAsync(It.IsAny<RepoProto.EmailExistsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmailFormat_ThrowsInvalidArgument()
    {
        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = "not-an-email" };

        var act = () => _sut.CreateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ThrowsAlreadyExists()
    {
        SetupEmailExists("jane@example.com", exists: true);
        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var act = () => _sut.CreateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.AlreadyExists);
        _repositoryClient.Verify(
            c => c.CreateUserAsync(It.IsAny<RepoProto.CreateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsUser()
    {
        var userId = Guid.NewGuid();
        SetupGetUserById(userId, new UserProto { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" });

        var reply = await _sut.GetUserById(new GetUserByIdRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        reply.User.UserId.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task GetUserById_WithInvalidGuid_ThrowsInvalidArgument()
    {
        var act = () => _sut.GetUserById(new GetUserByIdRequest { UserId = "not-a-guid" }, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
        _repositoryClient.Verify(
            c => c.GetUserByIdAsync(It.IsAny<RepoProto.GetUserByIdRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserById_WhenRepositoryThrowsNotFound_PropagatesNotFound()
    {
        var userId = Guid.NewGuid();
        _repositoryClient
            .Setup(c => c.GetUserByIdAsync(It.IsAny<RepoProto.GetUserByIdRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<RepoProto.UserReply>(new RpcException(new Status(StatusCode.NotFound, "not found"))));

        var act = () => _sut.GetUserById(new GetUserByIdRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_WithValidRequest_ReturnsUpdatedUser()
    {
        var userId = Guid.NewGuid();
        SetupGetUserById(userId, new UserProto { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" });
        SetupEmailExists("janet@example.com", exists: false);
        var updatedProto = new UserProto { UserId = userId.ToString(), FirstName = "Janet", LastName = "Doe", Email = "janet@example.com" };
        _repositoryClient
            .Setup(c => c.UpdateUserAsync(It.IsAny<RepoProto.UpdateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new RepoProto.UserReply { User = updatedProto }));

        var request = new UpdateUserRequest { UserId = userId.ToString(), FirstName = "Janet", LastName = "Doe", Email = "janet@example.com" };

        var reply = await _sut.UpdateUser(request, TestServerCallContext.Create());

        reply.User.FirstName.Should().Be("Janet");
    }

    [Fact]
    public async Task UpdateUser_WithMissingField_ThrowsInvalidArgument()
    {
        var request = new UpdateUserRequest { UserId = Guid.NewGuid().ToString(), FirstName = "", LastName = "Doe", Email = "jane@example.com" };

        var act = () => _sut.UpdateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task UpdateUser_WithInvalidGuid_ThrowsInvalidArgument()
    {
        var request = new UpdateUserRequest { UserId = "not-a-guid", FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var act = () => _sut.UpdateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task UpdateUser_WhenEmailUnchanged_DoesNotCheckDuplicateEmail()
    {
        var userId = Guid.NewGuid();
        SetupGetUserById(userId, new UserProto { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" });
        var updatedProto = new UserProto { UserId = userId.ToString(), FirstName = "Janet", LastName = "Doe", Email = "jane@example.com" };
        _repositoryClient
            .Setup(c => c.UpdateUserAsync(It.IsAny<RepoProto.UpdateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new RepoProto.UserReply { User = updatedProto }));

        var request = new UpdateUserRequest { UserId = userId.ToString(), FirstName = "Janet", LastName = "Doe", Email = "jane@example.com" };

        await _sut.UpdateUser(request, TestServerCallContext.Create());

        _repositoryClient.Verify(
            c => c.EmailExistsAsync(It.IsAny<RepoProto.EmailExistsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WhenEmailChangedToExistingEmail_ThrowsAlreadyExists()
    {
        var userId = Guid.NewGuid();
        SetupGetUserById(userId, new UserProto { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" });
        SetupEmailExists("taken@example.com", exists: true);

        var request = new UpdateUserRequest { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "taken@example.com" };

        var act = () => _sut.UpdateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.AlreadyExists);
        _repositoryClient.Verify(
            c => c.UpdateUserAsync(It.IsAny<RepoProto.UpdateUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFound_PropagatesNotFound()
    {
        var userId = Guid.NewGuid();
        _repositoryClient
            .Setup(c => c.GetUserByIdAsync(It.IsAny<RepoProto.GetUserByIdRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<RepoProto.UserReply>(new RpcException(new Status(StatusCode.NotFound, "not found"))));

        var request = new UpdateUserRequest { UserId = userId.ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var act = () => _sut.UpdateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_WithValidId_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        _repositoryClient
            .Setup(c => c.DeleteUserAsync(It.IsAny<RepoProto.DeleteUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new RepoProto.DeleteUserReply { Success = true }));

        var reply = await _sut.DeleteUser(new DeleteUserRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        reply.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_WithInvalidGuid_ThrowsInvalidArgument()
    {
        var act = () => _sut.DeleteUser(new DeleteUserRequest { UserId = "not-a-guid" }, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DeleteUser_WhenRepositoryThrowsNotFound_PropagatesNotFound()
    {
        var userId = Guid.NewGuid();
        _repositoryClient
            .Setup(c => c.DeleteUserAsync(It.IsAny<RepoProto.DeleteUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.CreateFaulted<RepoProto.DeleteUserReply>(new RpcException(new Status(StatusCode.NotFound, "not found"))));

        var act = () => _sut.DeleteUser(new DeleteUserRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task SearchUsers_ReturnsUsersFromRepository()
    {
        var repoReply = new RepoProto.SearchUsersReply();
        repoReply.Users.Add(new UserProto { UserId = Guid.NewGuid().ToString(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" });
        _repositoryClient
            .Setup(c => c.SearchUsersAsync(It.IsAny<RepoProto.SearchUsersRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(repoReply));

        var reply = await _sut.SearchUsers(new SearchUsersRequest { Name = "Doe" }, TestServerCallContext.Create());

        reply.Users.Should().ContainSingle(u => u.LastName == "Doe");
    }

    private void SetupEmailExists(string email, bool exists) =>
        _repositoryClient
            .Setup(c => c.EmailExistsAsync(
                It.Is<RepoProto.EmailExistsRequest>(r => r.Email == email),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new RepoProto.EmailExistsReply { Exists = exists }));

    private void SetupGetUserById(Guid userId, UserProto user) =>
        _repositoryClient
            .Setup(c => c.GetUserByIdAsync(
                It.Is<RepoProto.GetUserByIdRequest>(r => r.UserId == userId.ToString()),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncUnaryCallFactory.Create(new RepoProto.UserReply { User = user }));
}
