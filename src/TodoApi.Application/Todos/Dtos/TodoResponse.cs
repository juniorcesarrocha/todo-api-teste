using TodoApi.Domain.Enums;

namespace TodoApi.Application.Todos.Dtos;

public sealed record TodoResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    TodoPriority Priority,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<string> Tags);
