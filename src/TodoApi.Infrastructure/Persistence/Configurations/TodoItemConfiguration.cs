using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence.Configurations;

internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("Todos");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title)
            .HasMaxLength(TodoItem.TitleMaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(TodoItem.DescriptionMaxLength);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Tags ficam numa coluna JSON; o SQLite filtra com json_each. Linhas antigas recebem '[]'.
        builder.PrimitiveCollection(t => t.Tags)
            .IsRequired()
            .HasDefaultValueSql("'[]'");

        builder.HasIndex(t => t.IsCompleted);
        builder.HasIndex(t => t.CreatedAt);
    }
}
