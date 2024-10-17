using System.Runtime.ExceptionServices;
using Common;
using Common.Enums;
using Common.Exceptions;
using Newtonsoft.Json;

namespace LingualLoop.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Bir sonraki middleware'e geçiş yap
            await _next(context);
        }
        catch (Exception ex)
        {
            // Hata meydana gelirse buraya düşer
            var edInfo = ExceptionDispatchInfo.Capture(ex);
            await HandleExceptionAsync(context, ex, edInfo);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, ExceptionDispatchInfo edInfo)
    {
        try
        {
            LingualLoopException lingualLoopException = (edInfo.SourceException as LingualLoopException)!;
            var errorList = new
            {
                errorCode = lingualLoopException.ErrorList?.Select(x => x.ErrorCode).ToList(),
                errorDescription = lingualLoopException.ErrorList?.Select(x => x.ErrorDescription).ToList()
            };
            
            var error = new Dictionary<string, object?>
            {
                ["data"] = lingualLoopException.ErrorData,
                ["code"] = lingualLoopException.ErrorMessage?.ErrorCode.ToString(),
                ["errorCode"] = lingualLoopException.ErrorMessage?.Code,
                ["errorList"] = errorList,
                ["message"] = lingualLoopException.ErrorMessage?.Message,
                ["detailedMessage"] = lingualLoopException.InnerException?.Message,
            };
            
            int httpStatus = lingualLoopException.HttpStatusCode != null
                ? (int) lingualLoopException.HttpStatusCode
                : (!context.Response.HasStarted && context.Response.StatusCode == 200
                    ? 500
                    : context.Response.StatusCode);
            
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = httpStatus;
            
            await context.Response.WriteAsync(JsonConvert.SerializeObject(error));
            
            return;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        edInfo.Throw(); // Re-throw the original if we couldn't handle it
    }
}