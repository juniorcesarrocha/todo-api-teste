\## Tests (xUnit)



\- \*\*UnitTests\*\* references only Application (and Domain through it). It uses the hand-written fakes in `Fakes/`, `InMemoryTodoRepository` and `FixedTimeProvider`, with no mocking library.

\- \*\*IntegrationTests\*\* uses `TodoApiFactory` (`WebApplicationFactory<Program>`). It runs in the `Testing` environment, so startup migrations and OpenAPI are off. Each factory gets its own shared-cache in-memory SQLite database, kept alive by an open connection, and the factory runs the migrations itself in `CreateHost`. Changing the schema therefore needs a real migration, or the integration tests fail.

