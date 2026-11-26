using Microsoft.Extensions.Logging;
using TodoApi.Application.Abstractions;
using TodoApi.Application.Common;
using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Todos;

public sealed partial class TodoService(
    ITodoRepository repository,
    TimeProvider timeProvider,
    ILogger<TodoService> logger) : ITodoService
{
    public async Task<PagedResult<TodoResponse>> ListAsync(TodoQuery query, CancellationToken cancellationToken)
    {
        var normalized = query.Normalize();
        var (items, totalCount) = await repository.ListAsync(normalized, cancellationToken);

        return new PagedResult<TodoResponse>(
            [.. items.Select(item => item.ToResponse())],
            normalized.Page,
            normalized.PageSize,
            totalCount);
    }

    public async Task<TodoResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        return item?.ToResponse();
    }

    public async Task<TodoCountResponse> CountCompletedAsync(CancellationToken cancellationToken) =>
        new(await repository.CountCompletedAsync(cancellationToken));

    public async Task<TodoResponse?> GetOldestPendingAsync(CancellationToken cancellationToken)
    {
        var item = await repository.GetOldestPendingAsync(cancellationToken);
        return item?.ToResponse();
    }

    public async Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var item = TodoItem.Create(
            request.Title,
            request.Description,
            request.Priority,
            request.DueDate,
            request.Tags,
            timeProvider.GetUtcNow());

        repository.Add(item);
        await repository.SaveChangesAsync(cancellationToken);

        LogTodoCreated(item.Id);
        return item.ToResponse();
    }

    public async Task<TodoResponse?> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.Update(request.Title, request.Description, request.Priority, request.DueDate, request.Tags, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return item.ToResponse();
    }

    public Task<bool> CompleteAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, item => item.Complete(timeProvider.GetUtcNow()), cancellationToken);

    public Task<bool> ReopenAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, item => item.Reopen(timeProvider.GetUtcNow()), cancellationToken);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        repository.Remove(item);
        await repository.SaveChangesAsync(cancellationToken);

        LogTodoDeleted(id);
        return true;
    }

    private async Task<bool> ChangeAsync(Guid id, Action<TodoItem> change, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        change(item);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tarefa {TodoId} criada")]
    private partial void LogTodoCreated(Guid todoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tarefa {TodoId} removida")]
    private partial void LogTodoDeleted(Guid todoId);
}
