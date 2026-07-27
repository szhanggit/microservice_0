namespace Shared.Common.Validators;

public sealed class ValidationOutcome
{
    private ValidationOutcome(bool isValid, IReadOnlyCollection<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static ValidationOutcome Success() => new(true, Array.Empty<string>());

    public static ValidationOutcome Failure(IReadOnlyCollection<string> errors) => new(false, errors);
}
