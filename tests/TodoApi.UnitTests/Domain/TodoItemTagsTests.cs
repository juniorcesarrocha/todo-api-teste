using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace TodoApi.UnitTests.Domain;

public sealed class TodoItemTagsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    private static TodoItem CreateWithTags(params string?[]? tags) =>
        TodoItem.Create("Tarefa", null, TodoPriority.Medium, null, tags, Now);

    [Fact]
    public void Create_WithTags_NormalizesCaseAndTrims()
    {
        var item = CreateWithTags("Casa", " urgente ");

        Assert.Equal(["casa", "urgente"], item.Tags);
    }

    [Fact]
    public void Create_WithNullTags_HasEmptyTags()
    {
        var item = TodoItem.Create("Tarefa", null, TodoPriority.Medium, null, null, Now);

        Assert.Empty(item.Tags);
    }

    [Fact]
    public void Create_WithDuplicateTags_KeepsOne()
    {
        var item = CreateWithTags("casa", "Casa", "CASA");

        Assert.Equal(["casa"], item.Tags);
    }

    [Fact]
    public void Create_SortsTagsInOrdinalOrder()
    {
        var item = CreateWithTags("zeta", "alfa", "meio");

        Assert.Equal(["alfa", "meio", "zeta"], item.Tags);
    }

    [Fact]
    public void Create_WithAccentsDigitsHyphenUnderscore_Accepts()
    {
        var item = CreateWithTags("reunião-2026_q1");

        Assert.Equal(["reunião-2026_q1"], item.Tags);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankTag_Throws(string? tag)
    {
        var ex = Assert.Throws<DomainException>(() => CreateWithTags("casa", tag));

        Assert.Equal("A tag não pode ser vazia.", ex.Message);
    }

    [Fact]
    public void Create_WithTagTooLong_Throws()
    {
        var tag = new string('a', TodoItem.TagMaxLength + 1);

        var ex = Assert.Throws<DomainException>(() => CreateWithTags(tag));

        Assert.Equal($"A tag '{tag}' deve ter no máximo 30 caracteres.", ex.Message);
    }

    [Fact]
    public void Create_WithTagAtMaxLength_Accepts()
    {
        var tag = new string('a', 30);

        var item = CreateWithTags($"  {tag}  ");

        Assert.Equal([tag], item.Tags);
    }

    [Theory]
    [InlineData("a b")]
    [InlineData("#casa")]
    [InlineData("a.b")]
    public void Create_WithInvalidCharacters_Throws(string tag)
    {
        var ex = Assert.Throws<DomainException>(() => CreateWithTags(tag));

        Assert.Equal($"A tag '{tag}' contém caracteres inválidos. Use letras, dígitos, '-' ou '_'.", ex.Message);
    }

    [Fact]
    public void Create_WithMoreThanTenTags_Throws()
    {
        var tags = Enumerable.Range(1, 11).Select(i => $"t{i}").ToArray();

        var ex = Assert.Throws<DomainException>(() => CreateWithTags(tags));

        Assert.Equal("A tarefa pode ter no máximo 10 tags.", ex.Message);
    }

    [Fact]
    public void Create_WithElevenEntriesTenDistinct_Accepts()
    {
        var tags = Enumerable.Range(1, 10).Select(i => $"t{i}").Append("T1").ToArray();

        var item = CreateWithTags(tags);

        Assert.Equal(10, item.Tags.Count);
    }

    [Fact]
    public void Update_WithTags_ReplacesTags()
    {
        var item = CreateWithTags("casa", "urgente");

        item.Update("Tarefa", null, TodoPriority.Medium, null, ["trabalho"], Now.AddHours(1));

        Assert.Equal(["trabalho"], item.Tags);
    }

    [Fact]
    public void Update_WithEmptyTags_ClearsTags()
    {
        var item = CreateWithTags("casa");

        item.Update("Tarefa", null, TodoPriority.Medium, null, [], Now.AddHours(1));

        Assert.Empty(item.Tags);
    }

    [Fact]
    public void Update_WithNullTags_KeepsTags()
    {
        var item = CreateWithTags("casa", "urgente");

        item.Update("Outro título", null, TodoPriority.Medium, null, null, Now.AddHours(1));

        Assert.Equal(["casa", "urgente"], item.Tags);
        Assert.Equal("Outro título", item.Title);
    }

    [Fact]
    public void Update_WithInvalidTag_KeepsItemUnchanged()
    {
        var item = TodoItem.Create("Original", "Descrição", TodoPriority.Low, null, ["casa"], Now);

        Assert.Throws<DomainException>(() =>
            item.Update("Novo", "Nova descrição", TodoPriority.High, null, ["a b"], Now.AddHours(1)));

        Assert.Equal("Original", item.Title);
        Assert.Equal("Descrição", item.Description);
        Assert.Equal(TodoPriority.Low, item.Priority);
        Assert.Equal(["casa"], item.Tags);
        Assert.Equal(Now, item.UpdatedAt);
    }

    [Fact]
    public void CompleteAndReopen_KeepTags()
    {
        var item = CreateWithTags("casa", "urgente");

        item.Complete(Now.AddHours(1));
        Assert.Equal(["casa", "urgente"], item.Tags);

        item.Reopen(Now.AddHours(2));
        Assert.Equal(["casa", "urgente"], item.Tags);
    }
}
