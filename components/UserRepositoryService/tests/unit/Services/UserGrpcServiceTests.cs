using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Protos.UserRepository;
using UserRepositoryService.Entities;
using UserRepositoryService.Repositories;
using UserRepositoryService.Services;
using UserRepositoryService.Tests.Unit.TestUtilities;

namespace UserRepositoryService.Tests.Unit.Services;

public sealed class UserGrpcServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly UserGrpcService _sut;

    public UserGrpcServiceTests()
    {
        _sut = new UserGrpcService(_userRepository.Object, Mock.Of<ILogger<UserGrpcService>>());
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedUser()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var reply = await _sut.CreateUser(request, TestServerCallContext.Create());

        reply.User.FirstName.Should().Be("Jane");
        reply.User.LastName.Should().Be("Doe");
        reply.User.Email.Should().Be("jane@example.com");
        Guid.TryParse(reply.User.UserId, out _).Should().BeTrue();
        _userRepository.Verify(r => r.AddAsync(It.IsAny<UserInfo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithMissingFirstName_ThrowsInvalidArgument()
    {
        var request = new CreateUserRequest { FirstName = "", LastName = "Doe", Email = "jane@example.com" };

        var act = () => _sut.CreateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ThrowsAlreadyExists()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateUserRequest { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        var act = () => _sut.CreateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.AlreadyExists);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<UserInfo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ReturnsUser()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo { UserId = userId, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" });

        var reply = await _sut.GetUserById(new GetUserByIdRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        reply.User.UserId.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);

        var act = () => _sut.GetUserById(new GetUserByIdRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_WithInvalidGuid_ThrowsInvalidArgument()
    {
        var act = () => _sut.GetUserById(new GetUserByIdRequest { UserId = "not-a-guid" }, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task UpdateUser_WhenUserExists_ReturnsUpdatedUser()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.UpdateAsync(userId, "Jane", "Smith", "jane.smith@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo { UserId = userId, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" });

        var request = new UpdateUserRequest { UserId = userId.ToString(), FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" };

        var reply = await _sut.UpdateUser(request, TestServerCallContext.Create());

        reply.User.LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.UpdateAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);

        var request = new UpdateUserRequest { UserId = userId.ToString(), FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" };

        var act = () => _sut.UpdateUser(request, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var reply = await _sut.DeleteUser(new DeleteUserRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        reply.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _sut.DeleteUser(new DeleteUserRequest { UserId = userId.ToString() }, TestServerCallContext.Create());

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task SearchUsers_ReturnsMatchingUsers()
    {
        _userRepository.Setup(r => r.SearchByNameAsync("Doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" } });

        var reply = await _sut.SearchUsers(new SearchUsersRequest { Name = "Doe" }, TestServerCallContext.Create());

        reply.Users.Should().ContainSingle(u => u.LastName == "Doe");
    }

    [Fact]
    public async Task EmailExists_ReturnsRepositoryResult()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var reply = await _sut.EmailExists(new EmailExistsRequest { Email = "jane@example.com" }, TestServerCallContext.Create());

        reply.Exists.Should().BeTrue();
    }
}
