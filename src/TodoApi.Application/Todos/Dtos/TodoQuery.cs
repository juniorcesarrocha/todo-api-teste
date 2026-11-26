using TodoApi.Domain.Enums;

namespace TodoApi.Application.Todos.Dtos;

/// <summary>
/// Filtros e paginação da listagem de tarefas (lidos da query string).
/// Valores fora do intervalo permitido são normalizados pelo serviço.
/// </summary>
public sealed record TodoQuery(
    bool? IsCompleted = null,
    TodoPriority? Priority = null,
    string? Search = null,
    int Page = 1,
    int PageSize = TodoQuery.DefaultPageSize,
    string[]? Tags = null)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public TodoQuery Normalize() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        Tags = NormalizeTags(Tags)
    };

    // Mesma normalização das tags gravadas; valores em branco são ignorados.
    private static string[]? NormalizeTags(string[]? tags)
    {
        string[] normalized = [.. (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)];

        return normalized.Length == 0 ? null : normalized;
    }
}
