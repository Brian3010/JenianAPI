using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace JenianAPI.Errors
{
  public class GlobalExceptionHandler : IExceptionHandler
  {
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetails;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails,
      IHostEnvironment env) {
      _logger = logger;
      _problemDetails = problemDetails;
      _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {
      // Correlate logs with a request/activity id
      var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

      // Map well-known exceptions to HTTP status codes
      var (status, title, type, extensions) = exception switch {
        AppException appEx => (appEx.StatusCode, appEx.Message,
          "https://httpstatuses.com/" + appEx.StatusCode,
          new Dictionary<string, object?> {
            ["errorCode"] = appEx.ErrorCode,
            ["traceId"] = traceId,

          }),

        KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found",
          "https://httpstatuses.com/404",
          new Dictionary<string, object?> { ["traceId"] = traceId }),

        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized",
          "https://httpstatuses.com/401",
          new Dictionary<string, object?> { ["traceId"] = traceId }),

        _ => (StatusCodes.Status500InternalServerError, "Unexpected Error",
          "https://httpstatuses.com/500",
          new Dictionary<string, object?> { ["traceId"] = traceId })
      };

      // Log with full exception for observability
      _logger.LogError(exception, "Request failed with {Status}. TraceId={TraceId}", status, traceId);

      // Build RFC 7807 response
      var problem = new ProblemDetails {
        Title = title,
        Type = type,
        Status = status,
        Detail = _env.IsDevelopment() ? exception.ToString() : null,
        Instance = httpContext.Request.Path
      };
      foreach (var kv in extensions)
        problem.Extensions[kv.Key] = kv.Value;

      httpContext.Response.StatusCode = status;

      // Let the framework serialize the problem details (content-negotiated)
      return await _problemDetails.TryWriteAsync(new ProblemDetailsContext {
        HttpContext = httpContext,
        ProblemDetails = problem,
        Exception = exception
      });


    }
  }
}
