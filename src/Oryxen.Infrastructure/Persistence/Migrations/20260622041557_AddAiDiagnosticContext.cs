using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oryxen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiDiagnosticContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plant_diagnoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    DetectedPest = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    Recommendation = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plant_diagnoses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plant_diagnoses_PlantId",
                table: "plant_diagnoses",
                column: "PlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plant_diagnoses");
        }
    }
}
