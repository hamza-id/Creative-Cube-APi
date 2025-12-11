using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreativeCube.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBlueprintResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlueprintResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BlueprintId = table.Column<long>(type: "bigint", nullable: false),
                    ComplianceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Violations = table.Column<string>(type: "jsonb", nullable: true),
                    ExtractedData = table.Column<string>(type: "jsonb", nullable: true),
                    ReportUrl = table.Column<string>(type: "text", nullable: true),
                    AiRawResponse = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlueprintResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlueprintResults_Blueprints_BlueprintId",
                        column: x => x.BlueprintId,
                        principalTable: "Blueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlueprintResults_BlueprintId",
                table: "BlueprintResults",
                column: "BlueprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlueprintResults");
        }
    }
}
