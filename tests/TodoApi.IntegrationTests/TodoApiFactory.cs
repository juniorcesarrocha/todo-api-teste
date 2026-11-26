using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using TodoApi.Infrastructure;

namespace TodoApi.IntegrationTests;

/// <summary>
/// Sobe a API em memória usando um banco SQLite in-memory isolado por fábrica.
/// </summary>
public sealed class TodoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=todo-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAliveConnection;

    public TodoApiFactory()
    {
        // O banco in-memory só existe enquanto houver uma conexão aberta.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _connectionString);
        // Limite alto para que os testes que compartilham a fábrica não sejam barrados pelo rate limiting.
        builder.UseSetting("RateLimiting:PermitLimit", "100000");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        host.Services.MigrateDatabaseAsync().GetAwaiter().GetResult();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection.Dispose();
        }
    }
}
