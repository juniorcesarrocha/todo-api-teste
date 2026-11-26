using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Domain.Entities;

/// <summary>
/// Tarefa da lista de TODO. O estado só é alterado através dos métodos da entidade,
/// garantindo que as invariantes sejam sempre respeitadas.
/// </summary>
public sealed class TodoItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public TodoPriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Construtor exigido pelo EF Core.
    private TodoItem()
    {
    }

    public static TodoItem Create(
        string title,
        string? description,
        TodoPriority priority,
        DateOnly? dueDate,
        DateTimeOffset now)
    {
        var item = new TodoItem
        {
            Id = Guid.CreateVersion7(now),
            CreatedAt = now
        };

        item.ApplyChanges(title, description, priority, dueDate, now);
        return item;
    }

    public void Update(
        string title,
        string? description,
        TodoPriority priority,
        DateOnly? dueDate,
        DateTimeOffset now) =>
        ApplyChanges(title, description, priority, dueDate, now);

    public void Complete(DateTimeOffset now)
    {
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void Reopen(DateTimeOffset now)
    {
        if (!IsCompleted)
        {
            return;
        }

        IsCompleted = false;
        CompletedAt = null;
        UpdatedAt = now;
    }

    private void ApplyChanges(
        string title,
        string? description,
        TodoPriority priority,
        DateOnly? dueDate,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("O título da tarefa é obrigatório.");
        }

        title = title.Trim();
        if (title.Length > TitleMaxLength)
        {
            throw new DomainException($"O título deve ter no máximo {TitleMaxLength} caracteres.");
        }

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > DescriptionMaxLength)
        {
            throw new DomainException($"A descrição deve ter no máximo {DescriptionMaxLength} caracteres.");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new DomainException($"Prioridade inválida: {priority}.");
        }

        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        UpdatedAt = now;
    }
}
