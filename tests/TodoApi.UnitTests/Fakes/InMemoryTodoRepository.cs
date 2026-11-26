using TodoApi.Application.Abstractions;
using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Entities;

namespace TodoApi.UnitTests.Fakes;

internal sealed class InMemoryTodoRepository : ITodoRepository
{
    public List<TodoItem> Items { get; } = [];

    public int SaveChangesCount { get; private set; }

    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id));

    public Task<(IReadOnlyList<TodoItem> Items, int TotalCount)> ListAsync(TodoQuery query, CancellationToken cancellationToken)
    {
        var filtered = Items
            .Where(t => query.IsCompleted is null || t.IsCompleted == query.IsCompleted)
            .Where(t => query.Priority is null || t.Priority == query.Priority)
            .Where(t => query.Search is null || t.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            .Where(t => query.Tags is null || query.Tags.All(t.Tags.Contains))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        IReadOnlyList<TodoItem> page = [.. filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)];
        return Task.FromResult((page, filtered.Count));
    }

    public Task<int> CountCompletedAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Items.Count(t => t.IsCompleted));

    public Task<TodoItem?> GetOldestPendingAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Items
            .Where(t => !t.IsCompleted)
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .FirstOrDefault());

    public void Add(TodoItem item) => Items.Add(item);

    public void Remove(TodoItem item) => Items.Remove(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;
        return Task.CompletedTask;
    }
}
