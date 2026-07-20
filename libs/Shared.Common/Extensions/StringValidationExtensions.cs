using System.Net.Mail;

namespace Shared.Common.Extensions;

public static class StringValidationExtensions
{
    public static bool IsValidEmail(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
