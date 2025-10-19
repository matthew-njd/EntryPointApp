using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntryPointApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "WeeklyLogs");

            migrationBuilder.DropColumn(
                name: "Mileage",
                table: "WeeklyLogs");

            migrationBuilder.DropColumn(
                name: "OtherCharges",
                table: "WeeklyLogs");

            migrationBuilder.DropColumn(
                name: "ParkingFee",
                table: "WeeklyLogs");

            migrationBuilder.RenameColumn(
                name: "TollCharge",
                table: "WeeklyLogs",
                newName: "TollCharges");

            migrationBuilder.RenameColumn(
                name: "Hours",
                table: "WeeklyLogs",
                newName: "TotalHours");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "WeeklyLogs",
                newName: "DateTo");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateFrom",
                table: "WeeklyLogs",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WeeklyLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DailyLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WeeklyLogId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Mileage = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TollCharge = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ParkingFee = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    OtherCharges = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyLogs_WeeklyLogs_WeeklyLogId",
                        column: x => x.WeeklyLogId,
                        principalTable: "WeeklyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyLogs_UserId_DateFrom_DateTo",
                table: "WeeklyLogs",
                columns: new[] { "UserId", "DateFrom", "DateTo" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyLogs_UserId",
                table: "DailyLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyLogs_WeeklyLogId",
                table: "DailyLogs",
                column: "WeeklyLogId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyLogs_WeeklyLogId_Date",
                table: "DailyLogs",
                columns: new[] { "WeeklyLogId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyLogs");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyLogs_UserId_DateFrom_DateTo",
                table: "WeeklyLogs");

            migrationBuilder.DropColumn(
                name: "DateFrom",
                table: "WeeklyLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WeeklyLogs");

            migrationBuilder.RenameColumn(
                name: "TotalHours",
                table: "WeeklyLogs",
                newName: "Hours");

            migrationBuilder.RenameColumn(
                name: "TollCharges",
                table: "WeeklyLogs",
                newName: "TollCharge");

            migrationBuilder.RenameColumn(
                name: "DateTo",
                table: "WeeklyLogs",
                newName: "Date");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "WeeklyLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Mileage",
                table: "WeeklyLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCharges",
                table: "WeeklyLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ParkingFee",
                table: "WeeklyLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
