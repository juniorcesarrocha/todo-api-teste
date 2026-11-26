using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using TodoApi.Api.Endpoints;
using TodoApi.Api.Middleware;
using TodoApi.Api.RateLimiting;
using TodoApi.Application;
using TodoApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddTodoRateLimiting();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// O WebApplication já insere o UseRouting no início do pipeline, então as políticas por endpoint funcionam aqui.
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.Services.MigrateDatabaseAsync();
}

app.MapHealthChecks("/health");
app.MapTodoEndpoints();

await app.RunAsync();
