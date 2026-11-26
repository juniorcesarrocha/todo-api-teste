# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A TODO REST API on .NET 10 (minimal APIs, EF Core 10 + SQLite), using a Clean Architecture layout. Code comments, log messages, endpoint summaries and error messages are written in **Portuguese (pt-BR)**. Keep new ones in Portuguese too.

## Commands

The solution file is `TodoApi.slnx`. The shell is PowerShell, so chain commands with `;`, not `&&`.

```powershell
dotnet build                                   # TreatWarningsAsErrors=true (Directory.Build.props): warnings break the build
dotnet run --project src/TodoApi.Api           # Development: applies migrations on startup, serves /openapi and Scalar UI at /scalar
dotnet test                                    # all tests
dotnet test tests/TodoApi.UnitTests            # one project
dotnet test --filter "FullyQualifiedName~TodoServiceTests"          # one class
dotnet test --filter "FullyQualifiedName~TodoItemTests.Complete"    # one test (or a name prefix)
```

EF Core migrations: `dotnet-ef` is a local tool pinned in `dotnet-tools.json` at the repo root, so run `dotnet tool restore` first.

```powershell
dotnet ef migrations add <Name> --project src/TodoApi.Infrastructure --startup-project src/TodoApi.Api --output-dir Persistence/Migrations
```

`src/TodoApi.Api/TodoApi.Api.http` holds sample requests. The SQLite file `todo.db` is created in the API's working directory and is gitignored.

## Architecture

The dependency direction is `Api → Application → Domain` and `Api → Infrastructure → Application`. Each of Application and Infrastructure exposes an `AddApplication()` / `AddInfrastructure()` extension in its own `DependencyInjection.cs`, and `Program.cs` calls both.

- **Domain**: `TodoItem` is a rich entity with private setters and a private EF constructor. All state changes go through `Create` / `Update` / `Complete` / `Reopen`, which enforce invariants and throw `DomainException`. `Complete` and `Reopen` are idempotent. The current time is always passed in as `DateTimeOffset now`, and IDs are `Guid.CreateVersion7(now)`.
- **Application**: `TodoService` coordinates the work through `ITodoRepository`, which is defined here and implemented in Infrastructure. Its methods return `null` or `false` for "not found" instead of throwing. Time comes from an injected `TimeProvider`, registered with `TryAddSingleton(TimeProvider.System)` so tests can override it. DTOs are records in `Todos/Dtos`. Request DTOs carry DataAnnotations that reuse the entity's length constants. `TodoQuery.Normalize()` clamps paging (max page size 100). Logging uses source-generated `[LoggerMessage]` partial methods.
- **Infrastructure**: `TodoDbContext` picks up `IEntityTypeConfiguration`s from `Persistence/Configurations`. A global convention stores every `DateTimeOffset` as a binary/long so SQLite can sort and filter on it; changing this requires a migration. `TodoRepository` is `internal`, escapes LIKE wildcards in search, and sorts by `CreatedAt desc, Id desc`. The connection string `ConnectionStrings:TodoDb` is resolved lazily inside `AddDbContext` so tests can override it.
- **Api**: endpoints live in `Endpoints/TodoEndpoints.cs` under the `/api/v1/todos` map group. They use `TypedResults` with `Results<...>` union return types. `builder.Services.AddValidation()` (.NET 10 built-in minimal-API validation) validates the DataAnnotations on request DTOs. `GlobalExceptionHandler` maps `DomainException` → 400 and everything else → 500 as ProblemDetails, and leaves the detail out of 5xx responses. Enums are serialized as strings. A health check is served at `/health`.
