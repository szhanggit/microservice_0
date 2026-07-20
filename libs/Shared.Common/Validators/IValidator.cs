namespace Shared.Common.Validators;

public interface IValidator<in T>
{
    ValidationOutcome Validate(T instance);
}
