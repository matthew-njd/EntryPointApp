using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntryPointApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedEmailTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Timesheet_ApprovedEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddedByAdminId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_ApprovedEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_ApprovedEmails_Timesheet_Users_AddedByAdminId",
                        column: x => x.AddedByAdminId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_ApprovedEmails_AddedByAdminId",
                table: "Timesheet_ApprovedEmails",
                column: "AddedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_ApprovedEmails_Email",
                table: "Timesheet_ApprovedEmails",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timesheet_ApprovedEmails");
        }
    }
}
