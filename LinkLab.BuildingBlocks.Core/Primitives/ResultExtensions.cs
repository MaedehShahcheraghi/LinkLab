using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LinkLab.BuildingBlocks.Core.Primitives;

public static class ResultExtensions
{
    public static IActionResult ToProblemDetails(this Result result, HttpContext? httpContext = null)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to problem details.");
        }

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation   => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
            ErrorType.NotFound     => StatusCodes.Status404NotFound,
            ErrorType.Conflict     => StatusCodes.Status409Conflict,
            _                      => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status    = statusCode,
            Title     = GetTitle(result.Error.Type),
            Type      = GetTypeUri(result.Error.Type),
            Detail    = result.Error.Details,
            Extensions =
            {
                ["code"]      = result.Error.Code,
                ["message"]   = result.Error.Description,
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["traceId"]   = httpContext?.TraceIdentifier
            }
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static string GetTitle(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation   => "Bad Request",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden    => "Forbidden",
            ErrorType.NotFound     => "Not Found",
            ErrorType.Conflict     => "Conflict",
            _                      => "Server Error"
        };

    private static string GetTypeUri(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation   => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
            ErrorType.Forbidden    => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            ErrorType.NotFound     => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict     => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _                      => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
}
