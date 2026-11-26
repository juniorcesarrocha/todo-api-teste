using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Abstractions;
using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Persistence;

namespace TodoApi.Infrastructure.Repositories;

internal sealed class TodoRepository(TodoDbContext dbContext) : ITodoRepository
{
    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<TodoItem> Items, int TotalCount)> ListAsync(
        TodoQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<TodoItem> todos = dbContext.Todos.AsNoTracking();

        if (query.IsCompleted is { } isCompleted)
        {
            todos = todos.Where(t => t.IsCompleted == isCompleted);
        }

        if (query.Priority is { } priority)
        {
            todos = todos.Where(t => t.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            todos = todos.Where(t =>
                EF.Functions.Like(t.Title, pattern, "\\") ||
                (t.Description != null && EF.Functions.Like(t.Description, pattern, "\\")));
        }

        var totalCount = await todos.CountAsync(cancellationToken);

        var items = await todos
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountCompletedAsync(CancellationToken cancellationToken) =>
        dbContext.Todos.CountAsync(t => t.IsCompleted, cancellationToken);

    public Task<TodoItem?> GetOldestPendingAsync(CancellationToken cancellationToken) =>
        dbContext.Todos
            .AsNoTracking()
            .Where(t => !t.IsCompleted)
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(TodoItem item) => dbContext.Todos.Add(item);

    public void Remove(TodoItem item) => dbContext.Todos.Remove(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
