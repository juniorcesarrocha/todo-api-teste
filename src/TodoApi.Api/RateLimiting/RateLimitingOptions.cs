using System.ComponentModel.DataAnnotations;

namespace TodoApi.Api.RateLimiting;

/// <summary>
/// Limites da política de rate limiting dos endpoints de tarefas (seção "RateLimiting").
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Número máximo de requisições por janela, por cliente.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 100;

    /// <summary>Duração da janela fixa, em segundos.</summary>
    [Range(1, 86_400)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Requisições excedentes que aguardam na fila em vez de serem rejeitadas (0 desliga a fila).</summary>
    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; }
}
