using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApi.Application.Common;
using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Enums;

namespace TodoApi.IntegrationTests;

/// <summary>
/// Critérios da feature de tags (.specs/features/todo-tags/spec.md), verificados pela API.
/// O banco é compartilhado entre os testes da classe, então cada teste usa títulos e tags únicos.
/// </summary>
public sealed class TodoTagsEndpointsTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
    private const string BaseUrl = "/api/v1/todos";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    // TAG-01
    [Fact]
    public async Task Post_WithTags_ReturnsNormalizedTags()
    {
        var response = await PostAsync(new CreateTodoRequest("Tarefa", null, Tags: ["Casa", " urgente "]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await ReadTodoAsync(response);
        Assert.Equal(["casa", "urgente"], todo.Tags);
    }

    // TAG-02
    [Fact]
    public async Task Post_WithoutTags_ReturnsEmptyTags()
    {
        var response = await PostRawAsync("""{ "title": "Sem tags" }""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"tags\":[]", json, StringComparison.Ordinal);
    }

    // TAG-03
    [Fact]
    public async Task Post_WithDuplicateTags_KeepsOne()
    {
        var created = await CreateAsync("Repetidas", ["casa", "Casa", "CASA"]);

        Assert.Equal(["casa"], created.Tags);
        Assert.Equal(["casa"], (await GetAsync(created.Id)).Tags);
    }

    // TAG-04
    [Fact]
    public async Task AllResponses_ReturnTagsInOrdinalOrder()
    {
        // Banco próprio para que /pending/oldest devolva a tarefa deste teste.
        await using var isolatedFactory = new TodoApiFactory();
        using var client = isolatedFactory.CreateClient();
        var title = $"Ordem {Uid()}";

        var createResponse = await client.PostAsJsonAsync(
            BaseUrl, new CreateTodoRequest(title, null, Tags: ["zeta", "alfa", "meio"]), JsonOptions);
        var created = await ReadTodoAsync(createResponse);
        Assert.Equal(["alfa", "meio", "zeta"], created.Tags);

        var updateResponse = await client.PutAsJsonAsync(
            $"{BaseUrl}/{created.Id}",
            new UpdateTodoRequest(title, null, TodoPriority.Medium, null, ["zz", "b", "a"]),
            JsonOptions);
        Assert.Equal(["a", "b", "zz"], (await ReadTodoAsync(updateResponse)).Tags);

        var fetched = await client.GetFromJsonAsync<TodoResponse>($"{BaseUrl}/{created.Id}", JsonOptions);
        Assert.Equal(["a", "b", "zz"], fetched!.Tags);

        var list = await client.GetFromJsonAsync<PagedResult<TodoResponse>>($"{BaseUrl}?search={title}", JsonOptions);
        Assert.Equal(["a", "b", "zz"], Assert.Single(list!.Items).Tags);

        var oldest = await client.GetFromJsonAsync<TodoResponse>($"{BaseUrl}/pending/oldest", JsonOptions);
        Assert.Equal(["a", "b", "zz"], oldest!.Tags);
    }

    // TAG-05
    [Fact]
    public async Task Put_WithTags_ReplacesTags()
    {
        var created = await CreateAsync("Substituir", ["casa", "urgente"]);

        var response = await PutAsync(created.Id, new UpdateTodoRequest("Substituir", null, TodoPriority.Medium, null, ["trabalho"]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["trabalho"], (await ReadTodoAsync(response)).Tags);
        Assert.Equal(["trabalho"], (await GetAsync(created.Id)).Tags);
    }

    // TAG-06
    [Fact]
    public async Task Put_WithEmptyTags_ClearsTags()
    {
        var created = await CreateAsync("Limpar", ["casa"]);

        var response = await PutAsync(created.Id, new UpdateTodoRequest("Limpar", null, TodoPriority.Medium, null, []));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await ReadTodoAsync(response)).Tags);
        Assert.Empty((await GetAsync(created.Id)).Tags);
    }

    // TAG-07
    [Theory]
    [InlineData("""{ "title": "Manter", "priority": "Medium" }""")]
    [InlineData("""{ "title": "Manter", "priority": "Medium", "tags": null }""")]
    public async Task Put_WithoutTags_KeepsTags(string body)
    {
        var created = await CreateAsync("Manter", ["casa", "urgente"]);

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"{BaseUrl}/{created.Id}", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["casa", "urgente"], (await ReadTodoAsync(response)).Tags);
        Assert.Equal(["casa", "urgente"], (await GetAsync(created.Id)).Tags);
    }

    // TAG-08
    [Fact]
    public async Task Get_AfterPost_ReturnsPersistedTags()
    {
        var created = await CreateAsync("Persistir", ["casa", "urgente"]);

        // Um cliente novo garante que a leitura vem do banco, não de estado da requisição anterior.
        using var otherClient = factory.CreateClient();
        var fetched = await otherClient.GetFromJsonAsync<TodoResponse>($"{BaseUrl}/{created.Id}", JsonOptions);

        Assert.Equal(["casa", "urgente"], fetched!.Tags);
    }

    // TAG-09
    [Fact]
    public async Task Post_WithAccentsDigitsHyphenUnderscore_Accepts()
    {
        var response = await PostAsync(new CreateTodoRequest("Acentos", null, Tags: ["reunião-2026_q1"]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(["reunião-2026_q1"], (await ReadTodoAsync(response)).Tags);
    }

    // TAG-10
    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("null")]
    public async Task Post_WithBlankTag_Returns400(string tagJson)
    {
        var title = $"Vazia {Uid()}";

        var response = await PostRawAsync($$"""{ "title": "{{title}}", "tags": ["casa", {{tagJson}}] }""");

        await AssertProblemAsync(response, "A tag não pode ser vazia.");
        Assert.Equal(0, (await ListAsync($"?search={title}")).TotalCount);
    }

    // TAG-11
    [Fact]
    public async Task Post_WithTagTooLong_Returns400()
    {
        var tag = new string('a', 31);

        var response = await PostAsync(new CreateTodoRequest("Longa", null, Tags: [tag]));

        await AssertProblemAsync(response, $"A tag '{tag}' deve ter no máximo 30 caracteres.");
    }

    // TAG-12
    [Fact]
    public async Task Post_WithTagAtMaxLength_Accepts()
    {
        var tag = new string('b', 30);

        var response = await PostAsync(new CreateTodoRequest("Limite", null, Tags: [tag]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal([tag], (await ReadTodoAsync(response)).Tags);
    }

    // TAG-13
    [Theory]
    [InlineData("a b")]
    [InlineData("#casa")]
    [InlineData("a.b")]
    public async Task Post_WithInvalidCharacters_Returns400(string tag)
    {
        var response = await PostAsync(new CreateTodoRequest("Caracteres", null, Tags: [tag]));

        await AssertProblemAsync(response, $"A tag '{tag}' contém caracteres inválidos. Use letras, dígitos, '-' ou '_'.");
    }

    // TAG-14
    [Fact]
    public async Task Post_WithMoreThanTenTags_Returns400()
    {
        string[] tags = [.. Enumerable.Range(1, 11).Select(i => $"t{i}")];

        var response = await PostAsync(new CreateTodoRequest("Muitas", null, Tags: tags));

        await AssertProblemAsync(response, "A tarefa pode ter no máximo 10 tags.");
    }

    // TAG-15
    [Fact]
    public async Task Post_WithElevenEntriesTenDistinct_Accepts()
    {
        string[] tags = [.. Enumerable.Range(1, 10).Select(i => $"t{i}"), "T1"];

        var response = await PostAsync(new CreateTodoRequest("Dez distintas", null, Tags: tags));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(10, (await ReadTodoAsync(response)).Tags.Count);
    }

    // TAG-16
    [Fact]
    public async Task Put_WithInvalidTag_KeepsTodoUnchanged()
    {
        var createResponse = await PostAsync(new CreateTodoRequest("Original", "Descrição", TodoPriority.Low, Tags: ["casa"]));
        var created = await ReadTodoAsync(createResponse);
        // Lido do banco, para comparar updatedAt na mesma precisão em que ele é gravado.
        var before = await GetAsync(created.Id);

        var response = await PutAsync(created.Id, new UpdateTodoRequest("Novo", "Nova descrição", TodoPriority.High, null, ["a b"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fetched = await GetAsync(created.Id);
        Assert.Equal("Original", fetched.Title);
        Assert.Equal("Descrição", fetched.Description);
        Assert.Equal(TodoPriority.Low, fetched.Priority);
        Assert.Equal(["casa"], fetched.Tags);
        Assert.Equal(before.UpdatedAt, fetched.UpdatedAt);
    }

    // TAG-25
    [Fact]
    public async Task Post_WithTagsOfWrongType_Returns400()
    {
        var title = $"Tipo errado {Uid()}";

        var response = await PostRawAsync($$"""{ "title": "{{title}}", "tags": "casa" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, (await ListAsync($"?search={title}")).TotalCount);
    }

    // TAG-27
    [Fact]
    public async Task CompleteAndReopen_KeepTags()
    {
        var created = await CreateAsync("Concluir", ["casa", "urgente"]);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"{BaseUrl}/{created.Id}/complete", null)).StatusCode);
        Assert.Equal(["casa", "urgente"], (await GetAsync(created.Id)).Tags);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"{BaseUrl}/{created.Id}/reopen", null)).StatusCode);
        Assert.Equal(["casa", "urgente"], (await GetAsync(created.Id)).Tags);
    }

    // TAG-17
    [Fact]
    public async Task List_FilterByOneTag_ReturnsOnlyTagged()
    {
        var u = Uid();
        var casa = $"casa{u}";
        var a = await CreateAsync($"A {u}", [casa]);
        var b = await CreateAsync($"B {u}", [casa, $"urgente{u}"]);
        await CreateAsync($"C {u}", [$"trabalho{u}"]);
        await CreateAsync($"D {u}", []);

        var result = await ListAsync($"?tags={casa}");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([b.Id, a.Id], result.Items.Select(t => t.Id));
    }

    // TAG-18
    [Fact]
    public async Task List_FilterByTwoTags_RequiresAll()
    {
        var u = Uid();
        var casa = $"casa{u}";
        var urgente = $"urgente{u}";
        await CreateAsync($"A {u}", [casa]);
        var both = await CreateAsync($"B {u}", [casa, urgente]);
        await CreateAsync($"C {u}", [urgente]);

        var result = await ListAsync($"?tags={casa}&tags={urgente}");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(both.Id, Assert.Single(result.Items).Id);
    }

    // TAG-19
    [Fact]
    public async Task List_FilterNormalizesValue()
    {
        var u = Uid();
        var tagged = await CreateAsync($"A {u}", [$"casa{u}"]);
        await CreateAsync($"B {u}", [$"outra{u}"]);

        var result = await ListAsync($"?tags=%20CASA{u.ToUpperInvariant()}%20");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(tagged.Id, Assert.Single(result.Items).Id);
    }

    // TAG-20
    [Fact]
    public async Task List_FilterByTagsCombinesWithOtherFilters()
    {
        var u = Uid();
        var tag = $"casa{u}";
        var match = await CreateAsync($"Alvo-{u}", [tag], TodoPriority.High);
        var completed = await CreateAsync($"Alvo-{u}", [tag], TodoPriority.High);
        await _client.PatchAsync($"{BaseUrl}/{completed.Id}/complete", null);
        await CreateAsync($"Alvo-{u}", [tag], TodoPriority.Low);
        await CreateAsync($"Alvo-{u}", [], TodoPriority.High);
        await CreateAsync($"Outro-{u}", [tag], TodoPriority.High);

        var result = await ListAsync($"?tags={tag}&isCompleted=false&priority=High&search=Alvo-{u}");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(match.Id, Assert.Single(result.Items).Id);
    }

    // TAG-21
    [Fact]
    public async Task List_FilterByTags_PaginatesFilteredResults()
    {
        var u = Uid();
        var tag = $"casa{u}";
        var first = await CreateAsync($"A {u}", [tag]);
        await CreateAsync($"B {u}", []);
        var second = await CreateAsync($"C {u}", [tag]);
        await CreateAsync($"D {u}", []);
        var third = await CreateAsync($"E {u}", [tag]);

        var page1 = await ListAsync($"?tags={tag}&pageSize=2&page=1");
        var page2 = await ListAsync($"?tags={tag}&pageSize=2&page=2");

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal([third.Id, second.Id], page1.Items.Select(t => t.Id));
        Assert.Equal(3, page2.TotalCount);
        Assert.Equal([first.Id], page2.Items.Select(t => t.Id));
    }

    // TAG-22
    [Theory]
    [InlineData("&tags=")]
    [InlineData("&tags=%20")]
    public async Task List_WithBlankTagFilter_IgnoresFilter(string tagsQuery)
    {
        var u = Uid();
        await CreateAsync($"A {u}", [$"casa{u}"]);
        await CreateAsync($"B {u}", []);

        var result = await ListAsync($"?search={u}{tagsQuery}");

        Assert.Equal(2, result.TotalCount);
    }

    // TAG-23
    [Fact]
    public async Task List_FilterByUnknownTag_ReturnsEmpty()
    {
        var u = Uid();
        await CreateAsync($"A {u}", [$"casa{u}"]);

        var response = await _client.GetAsync($"{BaseUrl}?tags=ninguem{u}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<PagedResult<TodoResponse>>(JsonOptions))!;
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    // TAG-24
    [Fact]
    public async Task List_FilterMatchesWholeTagOnly()
    {
        var u = Uid();
        var exact = await CreateAsync($"A {u}", [$"{u}casa"]);
        await CreateAsync($"B {u}", [$"{u}casamento"]);

        var result = await ListAsync($"?tags={u}casa");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(exact.Id, Assert.Single(result.Items).Id);
    }

    // TAG-26
    [Fact]
    public async Task Delete_RemovesTaggedTodoFromFilter()
    {
        var u = Uid();
        var tag = $"unica{u}";
        var created = await CreateAsync($"A {u}", [tag]);
        Assert.Equal(1, (await ListAsync($"?tags={tag}")).TotalCount);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{BaseUrl}/{created.Id}")).StatusCode);

        Assert.Equal(0, (await ListAsync($"?tags={tag}")).TotalCount);
    }

    // TAG-28
    [Fact]
    public async Task List_FilterWithMoreThanTenDistinctTags_ReturnsEmpty()
    {
        var u = Uid();
        string[] tags = [.. Enumerable.Range(1, 11).Select(i => $"t{i}{u}")];
        await CreateAsync($"Dez {u}", [.. tags.Take(10)]);
        var many = string.Join("&", Enumerable.Range(1, 500).Select(i => $"tags=x{i}{u}"));

        var exactlyTen = await ListAsync($"?{string.Join("&", tags.Take(10).Select(t => $"tags={t}"))}");
        var eleven = await ListAsync($"?{string.Join("&", tags.Select(t => $"tags={t}"))}");
        var response = await _client.GetAsync($"{BaseUrl}?{many}");

        Assert.Equal(1, exactlyTen.TotalCount);
        Assert.Equal(0, eleven.TotalCount);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<PagedResult<TodoResponse>>(JsonOptions))!;
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    private static string Uid() => Guid.NewGuid().ToString("N")[..8];

    private static async Task AssertProblemAsync(HttpResponseMessage response, string expectedDetail)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedDetail, problem.RootElement.GetProperty("detail").GetString());
    }

    private static async Task<TodoResponse> ReadTodoAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> PostAsync(CreateTodoRequest request) =>
        _client.PostAsJsonAsync(BaseUrl, request, JsonOptions);

    private async Task<HttpResponseMessage> PostRawAsync(string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await _client.PostAsync(BaseUrl, content);
    }

    private Task<HttpResponseMessage> PutAsync(Guid id, UpdateTodoRequest request) =>
        _client.PutAsJsonAsync($"{BaseUrl}/{id}", request, JsonOptions);

    private async Task<TodoResponse> CreateAsync(string title, string[] tags, TodoPriority priority = TodoPriority.Medium) =>
        await ReadTodoAsync(await PostAsync(new CreateTodoRequest(title, null, priority, Tags: tags)));

    private async Task<TodoResponse> GetAsync(Guid id) =>
        (await _client.GetFromJsonAsync<TodoResponse>($"{BaseUrl}/{id}", JsonOptions))!;

    private async Task<PagedResult<TodoResponse>> ListAsync(string queryString) =>
        (await _client.GetFromJsonAsync<PagedResult<TodoResponse>>($"{BaseUrl}{queryString}", JsonOptions))!;
}
