using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace TodoApi.UnitTests.Domain;

public sealed class TodoItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_SetsPropertiesAndTrimsText()
    {
        var item = TodoItem.Create("  Comprar pão  ", "  ", TodoPriority.High, new DateOnly(2026, 12, 1), null, Now);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("Comprar pão", item.Title);
        Assert.Null(item.Description);
        Assert.Equal(TodoPriority.High, item.Priority);
        Assert.False(item.IsCompleted);
        Assert.Equal(Now, item.CreatedAt);
        Assert.Equal(Now, item.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_Throws(string title) =>
        Assert.Throws<DomainException>(() => TodoItem.Create(title, null, TodoPriority.Low, null, null, Now));

    [Fact]
    public void Create_WithTitleTooLong_Throws() =>
        Assert.Throws<DomainException>(() =>
            TodoItem.Create(new string('a', TodoItem.TitleMaxLength + 1), null, TodoPriority.Low, null, null, Now));

    [Fact]
    public void Create_WithUndefinedPriority_Throws() =>
        Assert.Throws<DomainException>(() => TodoItem.Create("Tarefa", null, (TodoPriority)99, null, null, Now));

    [Fact]
    public void Complete_ThenReopen_UpdatesState()
    {
        var item = TodoItem.Create("Tarefa", null, TodoPriority.Medium, null, null, Now);
        var later = Now.AddHours(1);

        item.Complete(later);

        Assert.True(item.IsCompleted);
        Assert.Equal(later, item.CompletedAt);

        item.Reopen(later.AddHours(1));

        Assert.False(item.IsCompleted);
        Assert.Null(item.CompletedAt);
        Assert.Equal(later.AddHours(1), item.UpdatedAt);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_KeepsOriginalCompletionDate()
    {
        var item = TodoItem.Create("Tarefa", null, TodoPriority.Medium, null, null, Now);
        item.Complete(Now.AddHours(1));

        item.Complete(Now.AddHours(5));

        Assert.Equal(Now.AddHours(1), item.CompletedAt);
    }
}
