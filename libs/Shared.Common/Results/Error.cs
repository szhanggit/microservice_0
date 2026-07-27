using Shared.Contracts.Enums;

namespace Shared.Common.Results;

public sealed record Error(ErrorCode Code, string Message)
{
    public static readonly Error None = new(ErrorCode.None, string.Empty);
}
