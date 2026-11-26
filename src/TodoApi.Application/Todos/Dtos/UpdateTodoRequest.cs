using System.ComponentModel.DataAnnotations;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Todos.Dtos;

public sealed record UpdateTodoRequest(
    [property: Required, StringLength(TodoItem.TitleMaxLength)] string Title,
    [property: StringLength(TodoItem.DescriptionMaxLength)] string? Description,
    TodoPriority Priority,
    DateOnly? DueDate);
