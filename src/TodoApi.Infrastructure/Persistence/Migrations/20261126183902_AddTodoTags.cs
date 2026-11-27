using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Todos",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "'[]'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Todos");
        }
    }
}
