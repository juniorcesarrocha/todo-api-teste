using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Abstractions;

public interface ITodoRepository
{
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<TodoItem> Items, int TotalCount)> ListAsync(TodoQuery query, CancellationToken cancellationToken);

    Task<int> CountCompletedAsync(CancellationToken cancellationToken);

    Task<TodoItem?> GetOldestPendingAsync(CancellationToken cancellationToken);

    void Add(TodoItem item);

    void Remove(TodoItem item);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
