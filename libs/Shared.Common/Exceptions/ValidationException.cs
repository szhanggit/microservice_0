namespace Shared.Common.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(IReadOnlyCollection<string> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyCollection<string> Errors { get; }
}
