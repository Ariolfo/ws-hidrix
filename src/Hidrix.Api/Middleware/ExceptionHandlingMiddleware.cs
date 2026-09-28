using System.Net;
using System.Text.Json;
using FluentValidation;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Models;

namespace Hidrix.Api.Middleware;

/// <summary>
/// Middleware que captura excepciones y las envuelve en ApiResponse.
/// Los errores se registran con Serilog (consola + SQL Server HidrtbLog).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Inicializa el middleware.
    /// </summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoca el siguiente middleware y maneja errores.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, message) = ex switch
        {
            ValidationException ve => ((int)HttpStatusCode.BadRequest,
                string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))),
            AppException ae => (ae.StatusCode, ae.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Error interno del servidor"),
        };

        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        var user = context.User.Identity?.Name ?? "anonymous";
        var traceId = context.TraceIdentifier;

        if (status >= 500)
        {
            _logger.LogError(
                ex,
                "Error no controlado · {Method} {Path} · User={User} · TraceId={TraceId} · Status={StatusCode}",
                method,
                path,
                user,
                traceId,
                status);
        }
        else
        {
            _logger.LogWarning(
                ex,
                "Error de aplicación · {Method} {Path} · User={User} · TraceId={TraceId} · {Message}",
                method,
                path,
                user,
                traceId,
                message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        var payload = ApiResponse<object>.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }
}
