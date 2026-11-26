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
    int PageSize = TodoQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public TodoQuery Normalize() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim()
    };
}
