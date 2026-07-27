using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Contracts.Requests;
using Shared.Contracts.Responses;
using UserManagementGateway.Controllers;
using UserManagementGateway.Services;

namespace UserManagementGateway.Tests.Unit.Controllers;

public sealed class UsersControllerTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _sut = new UsersController(_userService.Object);
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedAtAction()
    {
        var response = new UserResponse(Guid.NewGuid(), "Jane", "Doe", "jane@example.com");
        _userService.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _sut.CreateUser(new CreateUserRequest("Jane", "Doe", "jane@example.com"), CancellationToken.None);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UsersController.GetUserById));
        createdResult.RouteValues!["id"].Should().Be(response.UserId);
        createdResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ReturnsOkWithUser()
    {
        var userId = Guid.NewGuid();
        var response = new UserResponse(userId, "Jane", "Doe", "jane@example.com");
        _userService.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _sut.GetUserById(userId, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task UpdateUser_WithValidRequest_ReturnsOkWithUpdatedUser()
    {
        var userId = Guid.NewGuid();
        var response = new UserResponse(userId, "Janet", "Doe", "janet@example.com");
        _userService.Setup(s => s.UpdateUserAsync(userId, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _sut.UpdateUser(userId, new UpdateUserRequest("Janet", "Doe", "janet@example.com"), CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task DeleteUser_WithExistingUser_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        _userService.Setup(s => s.DeleteUserAsync(userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.DeleteUser(userId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SearchUsers_ReturnsOkWithUserList()
    {
        var response = new UserListResponse(new[] { new UserResponse(Guid.NewGuid(), "Jane", "Doe", "jane@example.com") });
        _userService.Setup(s => s.SearchUsersAsync("Doe", It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _sut.SearchUsers("Doe", CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }
}
