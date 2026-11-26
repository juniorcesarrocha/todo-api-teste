using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.Application.Todos;
using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.UnitTests.Fakes;

namespace TodoApi.UnitTests.Application;

public sealed class TodoServiceTagsTests
{
    private readonly InMemoryTodoRepository _repository = new();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly TodoService _service;

    public TodoServiceTagsTests() =>
        _service = new TodoService(_repository, _time, NullLogger<TodoService>.Instance);

    private static CancellationToken Ct => CancellationToken.None;

    [Fact]
    public async Task GetByIdAsync_ReturnsEntityTagsInOrder()
    {
        var item = TodoItem.Create("Tarefa", null, TodoPriority.Medium, null, ["zeta", "Alfa"], _time.Now);
        _repository.Add(item);

        var result = await _service.GetByIdAsync(item.Id, Ct);

        Assert.NotNull(result);
        Assert.Equal(["alfa", "zeta"], result.Tags);
    }

    [Fact]
    public async Task ListAsync_WithMoreThanTenDistinctTags_ReturnsEmptyWithoutQuerying()
    {
        string[] tags = [.. Enumerable.Range(1, 11).Select(i => $"t{i}")];
        _repository.Add(TodoItem.Create("Tarefa", null, TodoPriority.Medium, null, tags.Take(10), _time.Now));

        var result = await _service.ListAsync(new TodoQuery(Tags: tags), Ct);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_WithTags_ReturnsNormalizedTags()
    {
        var result = await _service.CreateAsync(
            new CreateTodoRequest("Tarefa", null, Tags: ["Casa", " urgente "]), Ct);

        Assert.Equal(["casa", "urgente"], result.Tags);
        Assert.Equal(["casa", "urgente"], _repository.Items.Single().Tags);
    }

    [Fact]
    public async Task CreateAsync_WithoutTags_ReturnsEmptyTags()
    {
        var result = await _service.CreateAsync(new CreateTodoRequest("Tarefa", null), Ct);

        Assert.Empty(result.Tags);
    }

    [Fact]
    public async Task UpdateAsync_WithTags_ReplacesTags()
    {
        var created = await _service.CreateAsync(
            new CreateTodoRequest("Tarefa", null, Tags: ["casa", "urgente"]), Ct);

        var updated = await _service.UpdateAsync(
            created.Id, new UpdateTodoRequest("Tarefa", null, TodoPriority.Medium, null, ["trabalho"]), Ct);

        Assert.NotNull(updated);
        Assert.Equal(["trabalho"], updated.Tags);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyTags_ClearsTags()
    {
        var created = await _service.CreateAsync(
            new CreateTodoRequest("Tarefa", null, Tags: ["casa"]), Ct);

        var updated = await _service.UpdateAsync(
            created.Id, new UpdateTodoRequest("Tarefa", null, TodoPriority.Medium, null, []), Ct);

        Assert.NotNull(updated);
        Assert.Empty(updated.Tags);
    }

    [Fact]
    public async Task UpdateAsync_WithNullTags_KeepsTags()
    {
        var created = await _service.CreateAsync(
            new CreateTodoRequest("Tarefa", null, Tags: ["casa", "urgente"]), Ct);

        var updated = await _service.UpdateAsync(
            created.Id, new UpdateTodoRequest("Outro", null, TodoPriority.Medium, null), Ct);

        Assert.NotNull(updated);
        Assert.Equal(["casa", "urgente"], updated.Tags);
    }
}
