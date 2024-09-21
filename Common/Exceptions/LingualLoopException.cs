using System.Net;
using Common.Enums;
using Common.Errors;
using Common.Extensions;
// using Logging;

namespace Common.Exceptions;

public class LingualLoopException: System.Exception
{
    public LingualLoopException(ErrorMessage errorMessage, string message, HttpStatusCode? httpStatusCode = null, System.Exception? innerException = null, PriorityType logLevel = PriorityType.Error,  object? errorData = null)
        : base(message, innerException)
    {
        this.ErrorMessage = errorMessage;
        this.HttpStatusCode = httpStatusCode;
        this.ErrorData = errorData;
        this.LogLevel = logLevel;
    }
        
    public LingualLoopException(ErrorCode errorCode, string message, HttpStatusCode? httpStatusCode = null, System.Exception? innerException = null, PriorityType logLevel = PriorityType.Error,  object? errorData = null)
        : base(message, innerException)
    {
        this.ErrorMessage = errorCode.CreateMessage();
        this.HttpStatusCode = httpStatusCode;
        this.ErrorData = errorData;
        this.LogLevel = logLevel;
    }
        
    public ErrorMessage? ErrorMessage { get; }
        
    public HttpStatusCode? HttpStatusCode { get; }
        
    public object? ErrorData { get; }
    
    public PriorityType? LogLevel { get; }

}