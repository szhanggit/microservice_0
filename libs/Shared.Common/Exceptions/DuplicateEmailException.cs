namespace Shared.Common.Exceptions;

public sealed class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string email) : base($"A user with email '{email}' already exists.")
    {
        Email = email;
    }

    public string Email { get; }
}
