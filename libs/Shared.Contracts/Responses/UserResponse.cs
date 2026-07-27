using Shared.Contracts.Dtos;

namespace Shared.Contracts.Responses;

public sealed record UserResponse(Guid UserId, string FirstName, string LastName, string Email)
{
    public static UserResponse FromDto(UserDto dto) => new(dto.UserId, dto.FirstName, dto.LastName, dto.Email);
}
