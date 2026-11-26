using System.Text.RegularExpressions;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Domain.Entities;

/// <summary>
/// Tarefa da lista de TODO. O estado só é alterado através dos métodos da entidade,
/// garantindo que as invariantes sejam sempre respeitadas.
/// </summary>
public sealed partial class TodoItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    public const int TagMaxLength = 30;
    public const int MaxTags = 10;

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public TodoPriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Tags normalizadas (minúsculas, sem repetição), em ordem alfabética ordinal.</summary>
    public IReadOnlyList<string> Tags { get; private set; } = [];

    // Construtor exigido pelo EF Core.
    private TodoItem()
    {
    }

    public static TodoItem Create(
        string title,
        string? description,
        TodoPriority priority,
        DateOnly? dueDate,
        IEnumerable<string?>? tags,
        DateTimeOffset now)
    {
        var item = new TodoItem
        {
            Id = Guid.CreateVersion7(now),
            CreatedAt = now
        };

        item.ApplyChanges(title, description, priority, dueDate, tags ?? [], now);
        return item;
    }

    /// <summary>
    /// Atualiza os dados da tarefa. Com <paramref name="tags"/> nulo, as tags atuais são mantidas.
    /// </summary>
    public void Update(
        string title,
        string? description,
        TodoPriority priority,
        DateOnly? dueDate,
        IEnumerable<string?>? tags,
        DateTimeOffset now) =>
        ApplyChanges(title, description, priority, dueDate, tags, now);

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
        IEnumerable<string?>? tags,
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

        // Valida tudo antes de atribuir: uma alteração rejeitada não deixa a entidade pela metade.
        var normalizedTags = tags is null ? Tags : NormalizeTags(tags);

        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        Tags = normalizedTags;
        UpdatedAt = now;
    }

    private static List<string> NormalizeTags(IEnumerable<string?> tags)
    {
        var normalized = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var raw in tags)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new DomainException("A tag não pode ser vazia.");
            }

            var tag = raw.Trim().ToLowerInvariant();
            if (tag.Length > TagMaxLength)
            {
                throw new DomainException($"A tag '{tag}' deve ter no máximo {TagMaxLength} caracteres.");
            }

            if (!TagPattern().IsMatch(tag))
            {
                throw new DomainException($"A tag '{tag}' contém caracteres inválidos. Use letras, dígitos, '-' ou '_'.");
            }

            normalized.Add(tag);
        }

        if (normalized.Count > MaxTags)
        {
            throw new DomainException($"A tarefa pode ter no máximo {MaxTags} tags.");
        }

        return [.. normalized];
    }

    [GeneratedRegex(@"^[\p{L}\p{Nd}_-]+$")]
    private static partial Regex TagPattern();
}
