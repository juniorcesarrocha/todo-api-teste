using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.Application.Todos;
using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Enums;
using TodoApi.UnitTests.Fakes;

namespace TodoApi.UnitTests.Application;

public sealed class TodoServiceTests
{
    private readonly InMemoryTodoRepository _repository = new();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
    private readonly TodoService _service;

    public TodoServiceTests() =>
        _service = new TodoService(_repository, _time, NullLogger<TodoService>.Instance);

    private static CancellationToken Ct => CancellationToken.None;

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsTodo()
    {
        var result = await _service.CreateAsync(
            new CreateTodoRequest("Estudar .NET", "Minimal APIs", TodoPriority.High), Ct);

        Assert.Single(_repository.Items);
        Assert.Equal(1, _repository.SaveChangesCount);
        Assert.Equal("Estudar .NET", result.Title);
        Assert.Equal(_time.Now, result.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull() =>
        Assert.Null(await _service.GetByIdAsync(Guid.NewGuid(), Ct));

    [Fact]
    public async Task UpdateAsync_ChangesFieldsAndTimestamp()
    {
        var created = await _service.CreateAsync(new CreateTodoRequest("Original", null), Ct);
        _time.Now = _time.Now.AddMinutes(10);

        var updated = await _service.UpdateAsync(
            created.Id, new UpdateTodoRequest("Novo título", "Desc", TodoPriority.Low, null), Ct);

        Assert.NotNull(updated);
        Assert.Equal("Novo título", updated.Title);
        Assert.Equal(TodoPriority.Low, updated.Priority);
        Assert.Equal(_time.Now, updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull() =>
        Assert.Null(await _service.UpdateAsync(
            Guid.NewGuid(), new UpdateTodoRequest("X", null, TodoPriority.Low, null), Ct));

    [Fact]
    public async Task CompleteAndReopen_ToggleCompletion()
    {
        var created = await _service.CreateAsync(new CreateTodoRequest("Tarefa", null), Ct);

        Assert.True(await _service.CompleteAsync(created.Id, Ct));
        Assert.True((await _service.GetByIdAsync(created.Id, Ct))!.IsCompleted);

        Assert.True(await _service.ReopenAsync(created.Id, Ct));
        Assert.False((await _service.GetByIdAsync(created.Id, Ct))!.IsCompleted);
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("reopen")]
    [InlineData("delete")]
    public async Task MutationsOnMissingTodo_ReturnFalseWithoutSaving(string operation)
    {
        var existing = await _service.CreateAsync(new CreateTodoRequest("Existente", null), Ct);
        var savesBefore = _repository.SaveChangesCount;
        var missingId = Guid.NewGuid();

        var found = operation switch
        {
            "complete" => await _service.CompleteAsync(missingId, Ct),
            "reopen" => await _service.ReopenAsync(missingId, Ct),
            _ => await _service.DeleteAsync(missingId, Ct)
        };

        Assert.False(found);
        Assert.Equal(savesBefore, _repository.SaveChangesCount);
        // A tarefa existente não pode ser afetada.
        var untouched = Assert.Single(_repository.Items);
        Assert.Equal(existing.Id, untouched.Id);
        Assert.False(untouched.IsCompleted);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTodo()
    {
        var created = await _service.CreateAsync(new CreateTodoRequest("Tarefa", null), Ct);

        Assert.True(await _service.DeleteAsync(created.Id, Ct));
        Assert.Empty(_repository.Items);
        Assert.False(await _service.DeleteAsync(created.Id, Ct));
    }

    [Fact]
    public async Task CountCompletedAsync_CountsOnlyCompletedTodos()
    {
        var first = await _service.CreateAsync(new CreateTodoRequest("Primeira", null), Ct);
        var second = await _service.CreateAsync(new CreateTodoRequest("Segunda", null), Ct);
        await _service.CreateAsync(new CreateTodoRequest("Pendente", null), Ct);
        await _service.CompleteAsync(first.Id, Ct);
        await _service.CompleteAsync(second.Id, Ct);

        var result = await _service.CountCompletedAsync(Ct);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetOldestPendingAsync_ReturnsOldestNotCompletedTodo()
    {
        var oldest = await _service.CreateAsync(new CreateTodoRequest("Mais antiga", null), Ct);
        _time.Now = _time.Now.AddMinutes(1);
        var middle = await _service.CreateAsync(new CreateTodoRequest("Do meio", null), Ct);
        _time.Now = _time.Now.AddMinutes(1);
        await _service.CreateAsync(new CreateTodoRequest("Mais nova", null), Ct);
        await _service.CompleteAsync(oldest.Id, Ct);

        var result = await _service.GetOldestPendingAsync(Ct);

        Assert.Equal(middle.Id, result?.Id);
    }

    [Fact]
    public async Task GetOldestPendingAsync_WhenNoPendingTodos_ReturnsNull()
    {
        var created = await _service.CreateAsync(new CreateTodoRequest("Concluída", null), Ct);
        await _service.CompleteAsync(created.Id, Ct);

        Assert.Null(await _service.GetOldestPendingAsync(Ct));
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-5, 500, 1, TodoQuery.MaxPageSize)]
    [InlineData(3, 10, 3, 10)]
    public async Task ListAsync_NormalizesPaging(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _service.ListAsync(new TodoQuery(Page: page, PageSize: pageSize), Ct);

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
    }
}
