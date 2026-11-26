using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Conexa.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261126090000_EventActivities")]
public class EventActivities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventActivities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventActivities", x => x.Id);
                table.ForeignKey("FK_EventActivities_Events_EventId", x => x.EventId, "Events", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EventActivityPeople",
            columns: table => new
            {
                ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventActivityPeople", x => new { x.ActivityId, x.UserId });
                table.ForeignKey("FK_EventActivityPeople_EventActivities_ActivityId", x => x.ActivityId,
                    "EventActivities", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_EventActivityPeople_AspNetUsers_UserId", x => x.UserId,
                    "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_EventActivities_EventId_StartsAt", "EventActivities", new[] { "EventId", "StartsAt" });
        migrationBuilder.CreateIndex("IX_EventActivityPeople_UserId", "EventActivityPeople", "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EventActivityPeople");
        migrationBuilder.DropTable(name: "EventActivities");
    }
}
