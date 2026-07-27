namespace Shared.Contracts.Requests;

public sealed record CreateUserRequest(string FirstName, string LastName, string Email);
