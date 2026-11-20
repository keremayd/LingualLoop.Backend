using System.Runtime.ExceptionServices;
using Common;
using Common.Enums;
using Common.Exceptions;
using Newtonsoft.Json;

namespace Stelinda.Api.Middlewares;

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
            StelindaException? stelindaException = edInfo.SourceException as StelindaException;
            if (stelindaException is null)
            {
                _logger.LogError(exception, "Unhandled exception");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;

                await context.Response.WriteAsync(JsonConvert.SerializeObject(new Dictionary<string, object?>
                {
                    ["data"] = null,
                    ["code"] = "500",
                    ["errorCode"] = "UnhandledException",
                    ["message"] = exception.Message,
                    ["detailedMessage"] = exception.InnerException?.Message,
                }));

                return;
            }

            var errorList = new
            {
                errorCode = stelindaException.ErrorList?.Select(x => x.ErrorCode).ToList(),
                errorDescription = stelindaException.ErrorList?.Select(x => x.ErrorDescription).ToList()
            };
            
            var error = new Dictionary<string, object?>
            {
                ["data"] = stelindaException.ErrorData,
                ["code"] = stelindaException.ErrorMessage?.ErrorCode.ToString(),
                ["errorCode"] = stelindaException.ErrorMessage?.Code,
                ["errorList"] = errorList,
                ["message"] = stelindaException.ErrorMessage?.Message,
                ["detailedMessage"] = stelindaException.InnerException?.Message,
            };
            
            int httpStatus = stelindaException.HttpStatusCode != null
                ? (int) stelindaException.HttpStatusCode
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
