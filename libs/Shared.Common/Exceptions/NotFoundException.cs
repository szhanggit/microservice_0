namespace Shared.Common.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException ForUser(Guid userId) =>
        new($"User with id '{userId}' was not found.");
}
