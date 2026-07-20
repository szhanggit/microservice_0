namespace Shared.Contracts.Responses;

public sealed record UserListResponse(IReadOnlyCollection<UserResponse> Users);
