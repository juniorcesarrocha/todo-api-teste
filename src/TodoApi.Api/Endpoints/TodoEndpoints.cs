using Microsoft.AspNetCore.Http.HttpResults;
using TodoApi.Api.RateLimiting;
using TodoApi.Application.Common;
using TodoApi.Application.Todos;
using TodoApi.Application.Todos.Dtos;

namespace TodoApi.Api.Endpoints;

public static class TodoEndpoints
{
    private const string GetTodoByIdRouteName = "GetTodoById";

    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/todos")
            .WithTags("Todos")
            .RequireRateLimiting(RateLimitingExtensions.TodosPolicy)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/", ListAsync)
            .WithName("ListTodos")
            .WithSummary("Lista tarefas com filtros e paginação");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName(GetTodoByIdRouteName)
            .WithSummary("Obtém uma tarefa pelo id");

        group.MapGet("/completed/count", CountCompletedAsync)
            .WithName("CountCompletedTodos")
            .WithSummary("Conta as tarefas concluídas");

        group.MapGet("/pending/oldest", GetOldestPendingAsync)
            .WithName("GetOldestPendingTodo")
            .WithSummary("Obtém a tarefa pendente mais antiga");

        group.MapPost("/", CreateAsync)
            .WithName("CreateTodo")
            .WithSummary("Cria uma nova tarefa")
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTodo")
            .WithSummary("Atualiza os dados de uma tarefa")
            .ProducesValidationProblem();

        group.MapPatch("/{id:guid}/complete", CompleteAsync)
            .WithName("CompleteTodo")
            .WithSummary("Marca uma tarefa como concluída");

        group.MapPatch("/{id:guid}/reopen", ReopenAsync)
            .WithName("ReopenTodo")
            .WithSummary("Reabre uma tarefa concluída");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodo")
            .WithSummary("Remove uma tarefa");

        return app;
    }

    private static async Task<Ok<PagedResult<TodoResponse>>> ListAsync(
        [AsParameters] TodoQuery query,
        ITodoService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Results<Ok<TodoResponse>, NotFound>> GetByIdAsync(
        Guid id,
        ITodoService service,
        CancellationToken cancellationToken) =>
        await service.GetByIdAsync(id, cancellationToken) is { } todo
            ? TypedResults.Ok(todo)
            : TypedResults.NotFound();

    private static async Task<Ok<TodoCountResponse>> CountCompletedAsync(
        ITodoService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.CountCompletedAsync(cancellationToken));

    private static async Task<Results<Ok<TodoResponse>, NotFound>> GetOldestPendingAsync(
        ITodoService service,
        CancellationToken cancellationToken) =>
        await service.GetOldestPendingAsync(cancellationToken) is { } todo
            ? TypedResults.Ok(todo)
            : TypedResults.NotFound();

    private static async Task<CreatedAtRoute<TodoResponse>> CreateAsync(
        CreateTodoRequest request,
        ITodoService service,
        CancellationToken cancellationToken)
    {
        var todo = await service.CreateAsync(request, cancellationToken);
        return TypedResults.CreatedAtRoute(todo, GetTodoByIdRouteName, new { id = todo.Id });
    }

    private static async Task<Results<Ok<TodoResponse>, NotFound>> UpdateAsync(
        Guid id,
        UpdateTodoRequest request,
        ITodoService service,
        CancellationToken cancellationToken) =>
        await service.UpdateAsync(id, request, cancellationToken) is { } todo
            ? TypedResults.Ok(todo)
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> CompleteAsync(
        Guid id,
        ITodoService service,
        CancellationToken cancellationToken) =>
        ToNoContentOrNotFound(await service.CompleteAsync(id, cancellationToken));

    private static async Task<Results<NoContent, NotFound>> ReopenAsync(
        Guid id,
        ITodoService service,
        CancellationToken cancellationToken) =>
        ToNoContentOrNotFound(await service.ReopenAsync(id, cancellationToken));

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id,
        ITodoService service,
        CancellationToken cancellationToken) =>
        ToNoContentOrNotFound(await service.DeleteAsync(id, cancellationToken));

    private static Results<NoContent, NotFound> ToNoContentOrNotFound(bool found) =>
        found ? TypedResults.NoContent() : TypedResults.NotFound();
}
