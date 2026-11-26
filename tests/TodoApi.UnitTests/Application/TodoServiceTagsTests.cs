using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.Application.Todos;
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
}
