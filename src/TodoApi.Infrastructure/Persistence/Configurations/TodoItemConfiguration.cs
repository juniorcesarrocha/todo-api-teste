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

        // Temporário: as tags ainda não têm coluna. O mapeamento e a migration entram na T5 da feature de tags.
        builder.Ignore(t => t.Tags);

        builder.HasIndex(t => t.IsCompleted);
        builder.HasIndex(t => t.CreatedAt);
    }
}
