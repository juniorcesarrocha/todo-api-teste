using Microsoft.AspNetCore.Diagnostics;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Api.Middleware;

/// <summary>
/// Converte exceções não tratadas em respostas ProblemDetails (RFC 9457).
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Regra de negócio violada"),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Requisição inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno no servidor")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(exception);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                // Detalhes internos de erros 5xx não são expostos ao cliente.
                Detail = statusCode < StatusCodes.Status500InternalServerError ? exception.Message : null
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exceção não tratada")]
    private partial void LogUnhandledException(Exception exception);
}
