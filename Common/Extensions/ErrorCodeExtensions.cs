using Common.Enums;
using Common.Errors;

namespace Common.Extensions;

public static class ErrorCodeExtensions
{
    public static ErrorMessage CreateMessage(this ErrorCode errorCode, params object?[] args)
    {
        return new ErrorMessage(errorCode, errorCode.GetDescription(args));
    }
}   