using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApi.Application.Common;
using TodoApi.Application.Todos.Dtos;
using TodoApi.Domain.Enums;

namespace TodoApi.IntegrationTests;

public sealed class TodoEndpointsTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
    private const string BaseUrl = "/api/v1/todos";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithValidBody_Returns201WithLocation()
    {
        var response = await _client.PostAsJsonAsync(
            BaseUrl, new CreateTodoRequest("Nova tarefa", "Detalhes", TodoPriority.High, new DateOnly(2026, 12, 1)), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.NotNull(todo);
        Assert.Equal("Nova tarefa", todo.Title);
        Assert.Equal(TodoPriority.High, todo.Priority);
        Assert.EndsWith($"{BaseUrl}/{todo.Id}", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_SerializesPriorityAsString()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { title = "Enum", priority = "Low" });

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"priority\":\"Low\"", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "title": "" }""")]
    [InlineData("""{ "description": "sem título" }""")]
    public async Task Post_WithInvalidBody_Returns400ProblemDetails(string body)
    {
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(BaseUrl, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_WithTitleTooLong_Returns400()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { title = new string('x', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenMissing_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task FullLifecycle_CreateGetUpdateCompleteReopenDelete()
    {
        var created = await CreateAsync("Ciclo de vida");

        var fetched = await _client.GetFromJsonAsync<TodoResponse>($"{BaseUrl}/{created.Id}", JsonOptions);
        Assert.Equal(created.Id, fetched!.Id);

        var updateResponse = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created.Id}", new UpdateTodoRequest("Ciclo atualizado", null, TodoPriority.Low, null), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.Equal("Ciclo atualizado", updated!.Title);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"{BaseUrl}/{created.Id}/complete", null)).StatusCode);
        var completed = await _client.GetFromJsonAsync<TodoResponse>($"{BaseUrl}/{created.Id}", JsonOptions);
        Assert.True(completed!.IsCompleted);
        Assert.NotNull(completed.CompletedAt);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"{BaseUrl}/{created.Id}/reopen", null)).StatusCode);
        var reopened = await _client.GetFromJsonAsync<TodoResponse>($"{BaseUrl}/{created.Id}", JsonOptions);
        Assert.False(reopened!.IsCompleted);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{BaseUrl}/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{BaseUrl}/{created.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH-complete")]
    [InlineData("PATCH-reopen")]
    [InlineData("DELETE")]
    public async Task MutationsOnMissingTodo_Return404(string operation)
    {
        var url = $"{BaseUrl}/{Guid.NewGuid()}";

        var response = operation switch
        {
            "PUT" => await _client.PutAsJsonAsync(url, new UpdateTodoRequest("X", null, TodoPriority.Low, null), JsonOptions),
            "PATCH-complete" => await _client.PatchAsync($"{url}/complete", null),
            "PATCH-reopen" => await _client.PatchAsync($"{url}/reopen", null),
            _ => await _client.DeleteAsync(url)
        };

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersSearchesAndPaginates()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var first = await CreateAsync($"Lista {tag} A", TodoPriority.High);
        await CreateAsync($"Lista {tag} B", TodoPriority.Low);
        await CreateAsync($"Lista {tag} C", TodoPriority.High);
        await _client.PatchAsync($"{BaseUrl}/{first.Id}/complete", null);

        var all = await ListAsync($"?search={tag}");
        Assert.Equal(3, all.TotalCount);

        var high = await ListAsync($"?search={tag}&priority=High");
        Assert.Equal(2, high.TotalCount);

        var pending = await ListAsync($"?search={tag}&isCompleted=false");
        Assert.Equal(2, pending.TotalCount);
        Assert.DoesNotContain(pending.Items, t => t.Id == first.Id);

        var paged = await ListAsync($"?search={tag}&page=2&pageSize=2");
        Assert.Single(paged.Items);
        Assert.Equal(2, paged.TotalPages);
    }

    [Fact]
    public async Task CountCompleted_ReflectsCompleteAndReopen()
    {
        // O banco é compartilhado entre os testes da classe, então comparamos a variação da contagem.
        var before = await CountCompletedAsync();
        var todo = await CreateAsync("Contagem");
        await CreateAsync("Contagem pendente");

        await _client.PatchAsync($"{BaseUrl}/{todo.Id}/complete", null);
        Assert.Equal(before + 1, await CountCompletedAsync());

        await _client.PatchAsync($"{BaseUrl}/{todo.Id}/reopen", null);
        Assert.Equal(before, await CountCompletedAsync());
    }

    [Fact]
    public async Task GetOldestPending_ReturnsOldestNotCompletedOr404()
    {
        // Usa uma fábrica própria para ter um banco vazio, já que o da classe é compartilhado.
        await using var isolatedFactory = new TodoApiFactory();
        using var client = isolatedFactory.CreateClient();
        var url = $"{BaseUrl}/pending/oldest";

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);

        var first = await CreateAsync(client, "Primeira");
        var second = await CreateAsync(client, "Segunda");
        await client.PatchAsync($"{BaseUrl}/{first.Id}/complete", null);

        var oldest = await client.GetFromJsonAsync<TodoResponse>(url, JsonOptions);
        Assert.Equal(second.Id, oldest!.Id);

        await client.PatchAsync($"{BaseUrl}/{second.Id}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);
    }

    private async Task<int> CountCompletedAsync() =>
        (await _client.GetFromJsonAsync<TodoCountResponse>($"{BaseUrl}/completed/count", JsonOptions))!.Count;

    private Task<TodoResponse> CreateAsync(string title, TodoPriority priority = TodoPriority.Medium) =>
        CreateAsync(_client, title, priority);

    private static async Task<TodoResponse> CreateAsync(
        HttpClient client, string title, TodoPriority priority = TodoPriority.Medium)
    {
        var response = await client.PostAsJsonAsync(BaseUrl, new CreateTodoRequest(title, null, priority), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions))!;
    }

    private async Task<PagedResult<TodoResponse>> ListAsync(string queryString) =>
        (await _client.GetFromJsonAsync<PagedResult<TodoResponse>>($"{BaseUrl}{queryString}", JsonOptions))!;
}
