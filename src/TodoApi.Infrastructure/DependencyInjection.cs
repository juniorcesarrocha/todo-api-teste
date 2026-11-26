using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Application.Abstractions;
using TodoApi.Infrastructure.Persistence;
using TodoApi.Infrastructure.Repositories;

namespace TodoApi.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "TodoDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // A connection string é resolvida de forma tardia para permitir que testes a sobrescrevam.
        services.AddDbContext<TodoDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' não configurada.");

            options.UseSqlite(connectionString);
        });

        services.AddScoped<ITodoRepository, TodoRepository>();
        return services;
    }

    /// <summary>
    /// Aplica as migrations pendentes do banco de dados.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
