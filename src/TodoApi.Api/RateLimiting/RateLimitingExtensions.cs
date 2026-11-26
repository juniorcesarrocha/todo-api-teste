using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace TodoApi.Api.RateLimiting;

internal static partial class RateLimitingExtensions
{
    /// <summary>Nome da política aplicada ao grupo /api/v1/todos.</summary>
    public const string TodosPolicy = "todos";

    public static IServiceCollection AddTodoRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            // Janela fixa particionada pelo IP do cliente. As opções são lidas ao criar cada partição,
            // e não no registro, para que os testes possam sobrescrevê-las pela configuração.
            // Os contadores ficam em memória: com várias instâncias, cada uma aplica o limite separadamente.
            options.AddPolicy(TodosPolicy, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueLimit = settings.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);
        }

        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitingExtensions));
        LogRateLimitExceeded(logger, httpContext.Connection.RemoteIpAddress?.ToString(), httpContext.Request.Path.Value);

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Limite de requisições excedido",
                Detail = "Muitas requisições em pouco tempo. Tente novamente mais tarde."
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Limite de requisições excedido para o IP {IpAddress} em {Path}")]
    private static partial void LogRateLimitExceeded(ILogger logger, string? ipAddress, string? path);
}
