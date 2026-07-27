using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Requests;
using Shared.Contracts.Responses;
using UserManagementGateway.Services;

namespace UserManagementGateway.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await userService.GetUserByIdAsync(id, cancellationToken);
        return Ok(user);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.UpdateUserAsync(id, request, cancellationToken);
        return Ok(user);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserListResponse>> SearchUsers([FromQuery] string? name, CancellationToken cancellationToken)
    {
        var users = await userService.SearchUsersAsync(name, cancellationToken);
        return Ok(users);
    }
}
