namespace Shared.Contracts.Dtos;

public sealed record UserDto(Guid UserId, string FirstName, string LastName, string Email);
