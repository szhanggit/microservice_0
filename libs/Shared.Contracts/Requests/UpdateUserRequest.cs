namespace Shared.Contracts.Requests;

public sealed record UpdateUserRequest(string FirstName, string LastName, string Email);
