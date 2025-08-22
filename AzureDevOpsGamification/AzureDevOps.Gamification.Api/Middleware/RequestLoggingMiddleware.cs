using System.Diagnostics;

namespace AzureDevOps.Gamification.Api.Middleware;

/// <summary>
/// Middleware para logging estruturado de requests - facilita debugging em produção
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString()[..8];

        // Log do request inicial
        _logger.LogInformation("Request {RequestId} started: {Method} {Path}",
            requestId, context.Request.Method, context.Request.Path);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request {RequestId} failed with exception", requestId);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Log do response final com métricas
            _logger.LogInformation("Request {RequestId} completed: {StatusCode} in {ElapsedMs}ms",
                requestId, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}