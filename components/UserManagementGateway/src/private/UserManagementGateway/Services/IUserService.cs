using Shared.Contracts.Requests;
using Shared.Contracts.Responses;

namespace UserManagementGateway.Services;

public interface IUserService
{
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserResponse> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserListResponse> SearchUsersAsync(string? name, CancellationToken cancellationToken);
}
