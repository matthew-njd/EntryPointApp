using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntryPointApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalesRepComment",
                table: "Timesheet_WeeklyLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesRepId",
                table: "Timesheet_Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_Users_SalesRepId",
                table: "Timesheet_Users",
                column: "SalesRepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheet_Users_Timesheet_Users_SalesRepId",
                table: "Timesheet_Users",
                column: "SalesRepId",
                principalTable: "Timesheet_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                "UPDATE Timesheet_WeeklyLogs SET Status = 'PendingManager' WHERE Status = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheet_Users_Timesheet_Users_SalesRepId",
                table: "Timesheet_Users");

            migrationBuilder.DropIndex(
                name: "IX_Timesheet_Users_SalesRepId",
                table: "Timesheet_Users");

            migrationBuilder.DropColumn(
                name: "SalesRepComment",
                table: "Timesheet_WeeklyLogs");

            migrationBuilder.DropColumn(
                name: "SalesRepId",
                table: "Timesheet_Users");
        }
    }
}
