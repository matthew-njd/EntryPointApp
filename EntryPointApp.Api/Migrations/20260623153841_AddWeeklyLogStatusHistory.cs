using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntryPointApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyLogStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Timesheet_WeeklyLogStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeeklyLogId = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_WeeklyLogStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_WeeklyLogStatusHistories_Timesheet_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Timesheet_WeeklyLogStatusHistories_Timesheet_WeeklyLogs_WeeklyLogId",
                        column: x => x.WeeklyLogId,
                        principalTable: "Timesheet_WeeklyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_WeeklyLogStatusHistories_ActorId",
                table: "Timesheet_WeeklyLogStatusHistories",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_WeeklyLogStatusHistories_WeeklyLogId",
                table: "Timesheet_WeeklyLogStatusHistories",
                column: "WeeklyLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timesheet_WeeklyLogStatusHistories");
        }
    }
}
