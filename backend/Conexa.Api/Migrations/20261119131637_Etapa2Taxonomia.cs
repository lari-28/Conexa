using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conexa.Api.Migrations
{
    /// <inheritdoc />
    public partial class Etapa2Taxonomia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainThemes",
                table: "Events",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdealCustomer",
                table: "Companies",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterestTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Synonyms = table.Column<List<string>>(type: "text[]", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LibraryKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterestTags_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterestTags_InterestTags_ParentId",
                        column: x => x.ParentId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantEventProfiles",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Goal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Timeframe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ConsentMatchingAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsentShareWithExhibitorsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsentExternalAiAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantEventProfiles", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ParticipantEventProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantEventProfiles_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyTags",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTags", x => new { x.CompanyId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CompanyTags_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyTags_InterestTags_TagId",
                        column: x => x.TagId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantTags",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantTags", x => new { x.EventId, x.UserId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ParticipantTags_InterestTags_TagId",
                        column: x => x.TagId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantTags_ParticipantEventProfiles_EventId_UserId",
                        columns: x => new { x.EventId, x.UserId },
                        principalTable: "ParticipantEventProfiles",
                        principalColumns: new[] { "EventId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTags_TagId",
                table: "CompanyTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_InterestTags_EventId_ParentId_Name",
                table: "InterestTags",
                columns: new[] { "EventId", "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterestTags_EventId_Status",
                table: "InterestTags",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InterestTags_ParentId",
                table: "InterestTags",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantEventProfiles_UserId",
                table: "ParticipantEventProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantTags_TagId",
                table: "ParticipantTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyTags");

            migrationBuilder.DropTable(
                name: "ParticipantTags");

            migrationBuilder.DropTable(
                name: "InterestTags");

            migrationBuilder.DropTable(
                name: "ParticipantEventProfiles");

            migrationBuilder.DropColumn(
                name: "MainThemes",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IdealCustomer",
                table: "Companies");
        }
    }
}
