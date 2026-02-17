using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntryPointApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerCommentToWeeklyLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManagerComment",
                table: "WeeklyLogs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyLogs_Status",
                table: "WeeklyLogs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyLogs_Status",
                table: "WeeklyLogs");

            migrationBuilder.DropColumn(
                name: "ManagerComment",
                table: "WeeklyLogs");
        }
    }
}
