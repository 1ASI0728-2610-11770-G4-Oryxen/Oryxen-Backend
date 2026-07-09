using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oryxen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisReportingContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analysis_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RangeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RangeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FileContent = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_reports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_reports_UserAccountId",
                table: "analysis_reports",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_reports_UserAccountId_PlantId",
                table: "analysis_reports",
                columns: new[] { "UserAccountId", "PlantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_reports");
        }
    }
}
