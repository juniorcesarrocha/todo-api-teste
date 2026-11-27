using TodoApi.Application.Todos.Dtos;

namespace TodoApi.UnitTests.Application;

public sealed class TodoQueryTagsTests
{
    [Fact]
    public void Normalize_TrimsAndLowercasesTags() =>
        Assert.Equal(["casa"], Assert.IsType<string[]>(new TodoQuery(Tags: [" CASA "]).Normalize().Tags));

    [Fact]
    public void Normalize_WithOnlyBlankTags_ReturnsNull() =>
        Assert.Null(new TodoQuery(Tags: ["", " "]).Normalize().Tags);

    [Fact]
    public void Normalize_RemovesDuplicateTags() =>
        Assert.Equal(["a"], Assert.IsType<string[]>(new TodoQuery(Tags: ["a", "A"]).Normalize().Tags));

    [Fact]
    public void Normalize_WithoutTags_KeepsNull() =>
        Assert.Null(new TodoQuery().Normalize().Tags);
}
