using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.IntegrationTests;

public sealed class RateLimitingTests
{
    private const string BaseUrl = "/api/v1/todos";

    [Fact]
    public async Task TodoEndpoints_WhenLimitExceeded_Return429ProblemDetailsWithRetryAfter()
    {
        await using var baseFactory = new TodoApiFactory();
        await using var factory = WithLimit(baseFactory, permitLimit: 2);
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(BaseUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(BaseUrl)).StatusCode);

        var rejected = await client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task Health_IsNotRateLimited()
    {
        await using var baseFactory = new TodoApiFactory();
        await using var factory = WithLimit(baseFactory, permitLimit: 1);
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        }
    }

    // Janela longa para que ela não reinicie no meio do teste.
    private static WebApplicationFactory<Program> WithLimit(TodoApiFactory factory, int permitLimit) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:PermitLimit", permitLimit.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting("RateLimiting:WindowSeconds", "3600");
        });
}
