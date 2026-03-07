using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ZoneType",
                table: "Zones",
                type: "int",
                nullable: true);

            // backfill existing zones based on ZoneCode prefix and VersionNotes
            migrationBuilder.Sql(@"
                UPDATE [Zones] SET ZoneType = 2 WHERE ZoneCode LIKE 'BLK-%';
                UPDATE [Zones] SET ZoneType = 2 WHERE ZoneType IS NULL AND VersionNotes LIKE '%Blocks%';
                UPDATE [Zones] SET ZoneType = 0 WHERE ZoneType IS NULL AND VersionNotes IS NOT NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Zone_ZoneType",
                table: "Zones",
                column: "ZoneType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Zone_ZoneType",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "ZoneType",
                table: "Zones");
        }
    }
}
