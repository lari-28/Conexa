using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conexa.Api.Migrations
{
    /// <inheritdoc />
    public partial class Etapa3Matching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchScores",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    ThemeScore = table.Column<double>(type: "double precision", nullable: true),
                    TextScore = table.Column<double>(type: "double precision", nullable: true),
                    AudienceScore = table.Column<double>(type: "double precision", nullable: true),
                    Band = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Reasons = table.Column<List<string>>(type: "text[]", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchScores", x => new { x.EventId, x.UserId, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_MatchScores_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchScores_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchScores_CompanyId",
                table: "MatchScores",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchScores_EventId_CompanyId_Score",
                table: "MatchScores",
                columns: new[] { "EventId", "CompanyId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchScores_UserId",
                table: "MatchScores",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchScores");
        }
    }
}
