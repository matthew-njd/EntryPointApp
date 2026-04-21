using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntryPointApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    MileageRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByAdminId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRates_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRates_CreatedByAdminId",
                table: "UserRates",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRates_UserId",
                table: "UserRates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRates_UserId_EffectiveDate",
                table: "UserRates",
                columns: new[] { "UserId", "EffectiveDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRates");
        }
    }
}
