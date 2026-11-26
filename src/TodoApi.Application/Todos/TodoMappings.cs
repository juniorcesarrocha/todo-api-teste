using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Todos;

public static class TodoMappings
{
    public static TodoResponse ToResponse(this TodoItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.IsCompleted,
        item.Priority,
        item.DueDate,
        item.CreatedAt,
        item.UpdatedAt,
        item.CompletedAt);
}
