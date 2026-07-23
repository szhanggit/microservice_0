using Shared.Common.Extensions;
using Shared.Common.Validators;
using Shared.Contracts.Constants;
using Shared.Protos.UserManagement;

namespace UserManagementService.Validation;

public sealed class UpdateUserRequestValidator : IValidator<UpdateUserRequest>
{
    public ValidationOutcome Validate(UpdateUserRequest instance)
    {
        var errors = new List<string>();

        if (!Guid.TryParse(instance.UserId, out _))
        {
            errors.Add($"'{instance.UserId}' is not a valid user id.");
        }

        ValidateRequiredField(instance.FirstName, nameof(instance.FirstName), UserValidationConstants.FirstNameMaxLength, errors);
        ValidateRequiredField(instance.LastName, nameof(instance.LastName), UserValidationConstants.LastNameMaxLength, errors);
        ValidateEmail(instance.Email, errors);

        return errors.Count == 0 ? ValidationOutcome.Success() : ValidationOutcome.Failure(errors);
    }

    private static void ValidateRequiredField(string value, string fieldName, int maxLength, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"'{fieldName}' is required.");
            return;
        }

        if (value.Length > maxLength)
        {
            errors.Add($"'{fieldName}' must not exceed {maxLength} characters.");
        }
    }

    private static void ValidateEmail(string email, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("'Email' is required.");
            return;
        }

        if (email.Length > UserValidationConstants.EmailMaxLength)
        {
            errors.Add($"'Email' must not exceed {UserValidationConstants.EmailMaxLength} characters.");
            return;
        }

        if (!email.IsValidEmail())
        {
            errors.Add("'Email' is not a valid email address.");
        }
    }
}
