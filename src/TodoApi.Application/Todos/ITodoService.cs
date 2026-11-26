using TodoApi.Application.Common;
using TodoApi.Application.Todos.Dtos;

namespace TodoApi.Application.Todos;

/// <summary>
/// Casos de uso de tarefas. Métodos retornam <c>null</c>/<c>false</c> quando a tarefa não existe.
/// </summary>
public interface ITodoService
{
    Task<PagedResult<TodoResponse>> ListAsync(TodoQuery query, CancellationToken cancellationToken);

    Task<TodoResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TodoCountResponse> CountCompletedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retorna a tarefa pendente criada há mais tempo, ou <c>null</c> se não houver tarefas pendentes.
    /// </summary>
    Task<TodoResponse?> GetOldestPendingAsync(CancellationToken cancellationToken);

    Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken);

    Task<TodoResponse?> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken cancellationToken);

    Task<bool> CompleteAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ReopenAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
