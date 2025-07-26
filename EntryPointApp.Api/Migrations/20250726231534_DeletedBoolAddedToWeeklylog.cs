using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntryPointApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class DeletedBoolAddedToWeeklylog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WeeklyLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WeeklyLogs");
        }
    }
}
