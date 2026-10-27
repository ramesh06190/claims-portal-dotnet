using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClaimsPortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimNumber = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    ClaimantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimantId = table.Column<string>(type: "nchar(8)", fixedLength: true, maxLength: 8, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Program = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WeekEnding = table.Column<DateOnly>(type: "date", nullable: false),
                    WorkedThisWeek = table.Column<bool>(type: "bit", nullable: false),
                    GrossEarnings = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    AbleAndAvailable = table.Column<bool>(type: "bit", nullable: false),
                    WeeklyAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Claims",
                columns: new[] { "Id", "AbleAndAvailable", "ClaimNumber", "ClaimantId", "ClaimantName", "Email", "GrossEarnings", "Phone", "Program", "Status", "SubmittedAt", "WeekEnding", "WeeklyAmount", "WorkedThisWeek" },
                values: new object[,]
                {
                    { 1, false, "CL-024051", "40000000", "Avery Johnson", null, 40m, null, "UnemploymentInsurance", "PendingReview", new DateTimeOffset(new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 17), 370m, true },
                    { 2, true, "CL-024088", "40001913", "Jordan Smith", null, null, null, "WorkShare", "NeedsInformation", new DateTimeOffset(new DateTime(2026, 10, 11, 9, 1, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 10), 370m, false },
                    { 3, true, "CL-024125", "40003826", "Taylor Nguyen", null, null, null, "TradeAdjustment", "Approved", new DateTimeOffset(new DateTime(2026, 10, 4, 9, 2, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 3), 370m, false },
                    { 4, true, "CL-024162", "40005739", "Morgan Patel", null, 109m, null, "DisasterAssistance", "Denied", new DateTimeOffset(new DateTime(2026, 9, 27, 9, 3, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 26), 311m, true },
                    { 5, true, "CL-024199", "40007652", "Casey Martinez", null, null, null, "UnemploymentInsurance", "PendingReview", new DateTimeOffset(new DateTime(2026, 9, 20, 9, 4, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 19), 370m, false },
                    { 6, true, "CL-024236", "40009565", "Riley Thompson", null, null, null, "WorkShare", "NeedsInformation", new DateTimeOffset(new DateTime(2026, 9, 13, 9, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 12), 370m, false },
                    { 7, true, "CL-024273", "40011478", "Jamie Robinson", null, 178m, null, "TradeAdjustment", "Approved", new DateTimeOffset(new DateTime(2026, 9, 6, 9, 6, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 5), 242m, true },
                    { 8, false, "CL-024310", "40013391", "Quinn Walker", null, null, null, "DisasterAssistance", "Denied", new DateTimeOffset(new DateTime(2026, 8, 30, 9, 7, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 8, 29), 370m, false },
                    { 9, true, "CL-024347", "40015304", "Drew Hernandez", null, null, null, "UnemploymentInsurance", "PendingReview", new DateTimeOffset(new DateTime(2026, 8, 23, 9, 8, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 8, 22), 370m, false },
                    { 10, true, "CL-024384", "40017217", "Skyler Lee", null, 247m, null, "WorkShare", "NeedsInformation", new DateTimeOffset(new DateTime(2026, 8, 16, 9, 9, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 8, 15), 173m, true },
                    { 11, true, "CL-024421", "40019130", "Reese Clark", null, null, null, "TradeAdjustment", "Approved", new DateTimeOffset(new DateTime(2026, 10, 18, 9, 10, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 17), 370m, false },
                    { 12, true, "CL-024458", "40021043", "Rowan Lewis", null, null, null, "DisasterAssistance", "Denied", new DateTimeOffset(new DateTime(2026, 10, 11, 9, 11, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 10), 370m, false },
                    { 13, true, "CL-024495", "40022956", "Parker Young", null, 316m, null, "UnemploymentInsurance", "PendingReview", new DateTimeOffset(new DateTime(2026, 10, 4, 9, 12, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 3), 104m, true },
                    { 14, true, "CL-024532", "40024869", "Hayden King", null, null, null, "WorkShare", "NeedsInformation", new DateTimeOffset(new DateTime(2026, 9, 27, 9, 13, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 26), 370m, false },
                    { 15, false, "CL-024569", "40026782", "Emerson Wright", null, null, null, "TradeAdjustment", "Approved", new DateTimeOffset(new DateTime(2026, 9, 20, 9, 14, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 19), 370m, false },
                    { 16, true, "CL-024606", "40028695", "Finley Scott", null, 85m, null, "DisasterAssistance", "Denied", new DateTimeOffset(new DateTime(2026, 9, 13, 9, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 12), 335m, true },
                    { 17, true, "CL-024643", "40030608", "Sawyer Green", null, null, null, "UnemploymentInsurance", "PendingReview", new DateTimeOffset(new DateTime(2026, 9, 6, 9, 16, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 5), 370m, false },
                    { 18, true, "CL-024680", "40032521", "Dakota Adams", null, null, null, "WorkShare", "NeedsInformation", new DateTimeOffset(new DateTime(2026, 8, 30, 9, 17, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 8, 29), 370m, false },
                    { 19, true, "CL-024717", "40034434", "Elliot Baker", null, 154m, null, "TradeAdjustment", "Approved", new DateTimeOffset(new DateTime(2026, 8, 23, 9, 18, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 8, 22), 266m, true },
                    { 20, true, "CL-024754", "40036347", "Harper Hill", null, null, null, "DisasterAssistance", "Denied", new DateTimeOffset(new DateTime(2026, 8, 16, 9, 19, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 8, 15), 370m, false },
                    { 21, true, "CL-024791", "40038260", "Kendall Rivera", null, null, null, "UnemploymentInsurance", "PendingReview", new DateTimeOffset(new DateTime(2026, 10, 18, 9, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 17), 370m, false },
                    { 22, false, "CL-024828", "40040173", "Logan Campbell", null, 223m, null, "WorkShare", "NeedsInformation", new DateTimeOffset(new DateTime(2026, 10, 11, 9, 21, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 10), 197m, true },
                    { 23, true, "CL-024865", "40042086", "Peyton Mitchell", null, null, null, "TradeAdjustment", "Approved", new DateTimeOffset(new DateTime(2026, 10, 4, 9, 22, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 10, 3), 370m, false },
                    { 24, true, "CL-024902", "40043999", "Remy Carter", null, null, null, "DisasterAssistance", "Denied", new DateTimeOffset(new DateTime(2026, 9, 27, 9, 23, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 26), 370m, false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimNumber",
                table: "Claims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_OccurredAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
