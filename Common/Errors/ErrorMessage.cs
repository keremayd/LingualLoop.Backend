using Common.Enums;

namespace Common.Errors;

public class ErrorMessage
{
    private const string Prefix = "E";
    
    public string Code { get; private set; }
    
    public ErrorCode ErrorCode { get; private set; }
    
    public string Message { get; protected set; }

    internal ErrorMessage(ErrorCode errorCode, string message)
    {
        this.ErrorCode = errorCode;
        this.Code = Prefix + (int) errorCode;
        this.Message = message;
    }
    
    public override string ToString()
    {
        return this.Code + ": " + this.Message;
    }
}