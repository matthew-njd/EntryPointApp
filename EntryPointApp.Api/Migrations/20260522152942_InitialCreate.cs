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
            migrationBuilder.CreateTable(
                name: "Timesheet_PayrollSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: false),
                    PayrollDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_PayrollSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timesheet_Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_Users_Timesheet_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Timesheet_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    ReplacedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_RefreshTokens_Timesheet_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timesheet_UserRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MileageRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAdminId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_UserRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_UserRates_Timesheet_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Timesheet_UserRates_Timesheet_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timesheet_WeeklyLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalCharges = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ManagerComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_WeeklyLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_WeeklyLogs_Timesheet_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timesheet_DailyLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WeeklyLogId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeIn = table.Column<TimeOnly>(type: "time", nullable: false),
                    TimeOut = table.Column<TimeOnly>(type: "time", nullable: false),
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
                    table.PrimaryKey("PK_Timesheet_DailyLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_DailyLogs_Timesheet_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Timesheet_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Timesheet_DailyLogs_Timesheet_WeeklyLogs_WeeklyLogId",
                        column: x => x.WeeklyLogId,
                        principalTable: "Timesheet_WeeklyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timesheet_DailyLogAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyLogId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_DailyLogAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheet_DailyLogAttachments_Timesheet_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "Timesheet_DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_DailyLogAttachments_DailyLogId",
                table: "Timesheet_DailyLogAttachments",
                column: "DailyLogId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_DailyLogs_UserId",
                table: "Timesheet_DailyLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_DailyLogs_WeeklyLogId",
                table: "Timesheet_DailyLogs",
                column: "WeeklyLogId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_DailyLogs_WeeklyLogId_Date",
                table: "Timesheet_DailyLogs",
                columns: new[] { "WeeklyLogId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_PayrollSchedules_DateFrom",
                table: "Timesheet_PayrollSchedules",
                column: "DateFrom");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_RefreshTokens_UserId",
                table: "Timesheet_RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_UserRates_CreatedByAdminId",
                table: "Timesheet_UserRates",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_UserRates_UserId",
                table: "Timesheet_UserRates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_UserRates_UserId_EffectiveDate",
                table: "Timesheet_UserRates",
                columns: new[] { "UserId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_Users_Email",
                table: "Timesheet_Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_Users_ManagerId",
                table: "Timesheet_Users",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_Users_PasswordResetToken",
                table: "Timesheet_Users",
                column: "PasswordResetToken");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_Users_Role",
                table: "Timesheet_Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_WeeklyLogs_Status",
                table: "Timesheet_WeeklyLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_WeeklyLogs_UserId",
                table: "Timesheet_WeeklyLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_WeeklyLogs_UserId_DateFrom_DateTo",
                table: "Timesheet_WeeklyLogs",
                columns: new[] { "UserId", "DateFrom", "DateTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timesheet_DailyLogAttachments");

            migrationBuilder.DropTable(
                name: "Timesheet_PayrollSchedules");

            migrationBuilder.DropTable(
                name: "Timesheet_RefreshTokens");

            migrationBuilder.DropTable(
                name: "Timesheet_UserRates");

            migrationBuilder.DropTable(
                name: "Timesheet_DailyLogs");

            migrationBuilder.DropTable(
                name: "Timesheet_WeeklyLogs");

            migrationBuilder.DropTable(
                name: "Timesheet_Users");
        }
    }
}
