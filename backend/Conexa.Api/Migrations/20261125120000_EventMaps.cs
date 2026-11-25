using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Conexa.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261125120000_EventMaps")]
public class EventMaps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventMaps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                Image = table.Column<byte[]>(type: "bytea", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventMaps", x => x.Id);
                table.ForeignKey("FK_EventMaps_Events_EventId", x => x.EventId, "Events", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_EventMaps_EventId_CreatedAt", "EventMaps", new[] { "EventId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "EventMaps");
}
