using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaBot.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAllDayEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAllDay",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAllDay",
                table: "Events");
        }
    }
}
