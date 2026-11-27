using System.ComponentModel.DataAnnotations;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Todos.Dtos;

public sealed record CreateTodoRequest(
    [property: Required, StringLength(TodoItem.TitleMaxLength)] string Title,
    [property: StringLength(TodoItem.DescriptionMaxLength)] string? Description,
    TodoPriority Priority = TodoPriority.Medium,
    DateOnly? DueDate = null,
    IReadOnlyList<string?>? Tags = null);
